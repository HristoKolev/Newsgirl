namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading.Tasks;

    public class RpcEngine
    {
        private Dictionary<string, RpcRequestMetadata> metadataByRequestName;

        private Func<Task, Result<object>> convertReturnValue;

        public RpcEngine(RpcEngineOptions options)
        {
            this.Build(options);
        }

        public List<RpcRequestMetadata> Metadata { get; private set; }

        private void Build(RpcEngineOptions options)
        {
            const BindingFlags METHOD_FLAG = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;

            var markedMethods = options.PotentialHandlerTypes.SelectMany(type => type.GetMethods(METHOD_FLAG))
                .Where(info => info.GetCustomAttribute<RpcBindAttribute>() != null).ToList();

            var handlers = new List<RpcRequestMetadata>(markedMethods.Count);

            foreach (var markedMethod in markedMethods)
            {
                // validate method flags
                if (markedMethod.IsStatic)
                {
                    throw new DetailedLogException(
                        $"Static methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                if (!markedMethod.IsPublic)
                {
                    throw new DetailedLogException(
                        $"Only public methods can be bound as an RPC handlers. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                if (markedMethod.IsVirtual)
                {
                    throw new DetailedLogException(
                        $"Virtual methods cannot be bound as an RPC handlers. This includes abstract methods and methods that belong to interfaces. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                // ensure that both specified types are not null
                var bindAttribute = markedMethod.GetCustomAttribute<RpcBindAttribute>();

                if (bindAttribute!.RequestType == null)
                {
                    throw new DetailedLogException(
                        $"{nameof(RpcBindAttribute)} must have not null {nameof(RpcBindAttribute.RequestType)}. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                if (!bindAttribute.RequestType.IsClass)
                {
                    throw new DetailedLogException($"Request type {bindAttribute.RequestType.Name} must be a reference type.");
                }

                if (bindAttribute.ResponseType == null)
                {
                    throw new DetailedLogException(
                        $"{nameof(RpcBindAttribute)} must have not null {nameof(RpcBindAttribute.ResponseType)}. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                if (!bindAttribute.ResponseType.IsClass)
                {
                    throw new DetailedLogException($"Response type {bindAttribute.ResponseType.Name} must be a reference type.");
                }

                var metadata = new RpcRequestMetadata
                {
                    DeclaringType = markedMethod.DeclaringType,
                    HandlerMethod = markedMethod,
                    RequestType = bindAttribute.RequestType,
                    ResponseType = bindAttribute.ResponseType,
                    ParameterTypes = markedMethod.GetParameters().Select(x => x.ParameterType).ToList(),
                    SupplementalAttributes = new Dictionary<Type, RpcSupplementalAttribute>(),
                };

                // throw if the type is abstract
                if (metadata.DeclaringType!.IsAbstract)
                {
                    throw new DetailedLogException(
                        $"Methods in abstract classes cannot be bound as an RPC handlers. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                // throw if the type is a value type
                if (metadata.DeclaringType!.IsValueType)
                {
                    throw new DetailedLogException(
                        $"Methods in value types cannot be bound as an RPC handlers. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                }

                // throw on multiple handlers for the same request type 
                var duplicateMetadataEntry = handlers.FirstOrDefault(x => x.RequestType.Name == metadata.RequestType.Name);

                if (duplicateMetadataEntry != null)
                {
                    throw new DetailedLogException(
                        "Handler binding conflict. A request is bound to 2 or more handler methods." +
                        $"{metadata.RequestType.Name} => {duplicateMetadataEntry.DeclaringType.Name}.{duplicateMetadataEntry.HandlerMethod.Name} AND " +
                        $"{metadata.RequestType.Name} => {metadata.DeclaringType!.Name}.{metadata.HandlerMethod.Name}");
                }

                // validate parameter types
                var allowedParameterTypes = new List<Type> {metadata.RequestType};

                if (options.ParameterTypeWhitelist != null)
                {
                    allowedParameterTypes.AddRange(options.ParameterTypeWhitelist);
                }

                foreach (var parameter in metadata.ParameterTypes)
                {
                    if (!allowedParameterTypes.Contains(parameter))
                    {
                        throw new DetailedLogException(
                            $"Parameter of type {parameter.Name} is not supported for RPC methods. {markedMethod.DeclaringType!.Name}.{markedMethod.Name}");
                    }
                }

                // gather supplemental attributes
                var allSupplementalAttributes = metadata.DeclaringType!.GetCustomAttributes()
                    .Concat(metadata.HandlerMethod.GetCustomAttributes())
                    .Where(x => x is RpcSupplementalAttribute)
                    .Cast<RpcSupplementalAttribute>()
                    .ToList();

                foreach (var supplementalAttribute in allSupplementalAttributes)
                {
                    var type = supplementalAttribute.GetType();
                    metadata.SupplementalAttributes[type] = supplementalAttribute;
                }

                // validate return type
                var taskWrappedResponseType = typeof(Task<>).MakeGenericType(metadata.ResponseType);

                var taskAndResultWrappedResponseType = typeof(Task<>).MakeGenericType(typeof(Result<>).MakeGenericType(metadata.ResponseType));

                if (metadata.HandlerMethod.ReturnType != taskAndResultWrappedResponseType && metadata.HandlerMethod.ReturnType != taskWrappedResponseType)
                {
                    throw new DetailedLogException(
                        $"Only {nameof(Task)}<{metadata.ResponseType}> and {nameof(Task)}<{nameof(Result)}<{metadata.ResponseType}>> are supported for" +
                        $" handler method `{metadata.DeclaringType.Name}.{metadata.HandlerMethod.Name}`.");
                }

                // validate middleware types 
                if (options.MiddlewareTypes == null)
                {
                    metadata.MiddlewareTypes = new List<Type>();
                }
                else
                {
                    foreach (var middlewareType in options.MiddlewareTypes)
                    {
                        if (!typeof(RpcMiddleware).IsAssignableFrom(middlewareType))
                        {
                            throw new DetailedLogException($"Middleware type {middlewareType!.Name} does not implement {nameof(RpcMiddleware)}.");
                        }
                    }

                    metadata.MiddlewareTypes = options.MiddlewareTypes.ToList();
                }

                metadata.CompiledMethod = CompileHandlerWithMiddleware(metadata);

                handlers.Add(metadata);
            }

            this.Metadata = handlers.OrderBy(x => x.RequestType.Name).ToList();
            this.metadataByRequestName = handlers.ToDictionary(x => x.RequestType.Name, x => x);
            this.convertReturnValue = GetConvertReturnValue();
        }

        private static Func<Task, Result<object>> GetConvertReturnValue()
        {
            var method = new DynamicMethod("convertReturnValue", typeof(Result<object>), new[] {typeof(Task)});

            var il = method.GetILGenerator();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(Task<object>).GetProperty(nameof(Task<object>.Result))?.GetMethod!);

            var afterNullCheckLabel = il.DefineLabel();

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Brtrue_S, afterNullCheckLabel);
            il.Emit(OpCodes.Ldstr, "Handler method return values must not be null.");
            il.Emit(OpCodes.Newobj, typeof(DetailedLogException).GetConstructor(new[] {typeof(string)})!);
            il.Emit(OpCodes.Throw);

            il.MarkLabel(afterNullCheckLabel);

            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Isinst, typeof(Result));

            var nonResultLabel = il.DefineLabel();
            il.Emit(OpCodes.Brfalse_S, nonResultLabel);
            il.Emit(OpCodes.Callvirt, typeof(Result).GetMethod(nameof(Result.ToGeneralForm))!);
            il.Emit(OpCodes.Ret);

            il.MarkLabel(nonResultLabel);

            il.Emit(OpCodes.Call, typeof(Result).GetMethods()
                .First(x => x.Name == nameof(Result.Ok) && x.IsGenericMethod && x.GetParameters().Length == 1)
                .MakeGenericMethod(typeof(object)));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate<Func<Task, Result<object>>>();
        }

        private static RpcRequestDelegate CompileHandlerWithMiddleware(RpcRequestMetadata metadata)
        {
            var getInstance = typeof(InstanceProvider).GetMethod(nameof(InstanceProvider.Get), new[] {typeof(Type)});

            var typeBuilder = ReflectionEmmitHelper.ModuleBuilder.DefineType(
                "RpcMiddlewareDynamicType+" + Guid.NewGuid(),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed |
                TypeAttributes.Abstract | TypeAttributes.AutoClass | TypeAttributes.AnsiClass
            );

            var delegateFieldMap = new Dictionary<MethodInfo, FieldBuilder>();

            var executeHandler = typeBuilder.DefineMethod(
                "executeHandlerMethod",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(Task),
                new[] {typeof(RpcContext), typeof(InstanceProvider)}
            );

            var executeHandlerGen = executeHandler.GetILGenerator();

            executeHandlerGen.DeclareLocal(typeof(Task));

            // load the handler instance from the instanceProvider
            executeHandlerGen.Emit(OpCodes.Ldarg_1);
            executeHandlerGen.Emit(OpCodes.Ldtoken, metadata.DeclaringType);
            executeHandlerGen.Emit(OpCodes.Callvirt, getInstance!);

            foreach (var parameter in metadata.ParameterTypes)
            {
                if (parameter == metadata.RequestType)
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty(nameof(RpcContext.RequestMessage))?.GetMethod!);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty(nameof(RpcRequestMessage.Payload))?.GetMethod!);
                    executeHandlerGen.Emit(OpCodes.Castclass, parameter);
                }
                else
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty(nameof(RpcContext.HandlerParameters))?.GetMethod!);
                    executeHandlerGen.Emit(OpCodes.Ldtoken, parameter);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(Dictionary<Type, object>).GetMethod("get_Item")!);
                }
            }

            executeHandlerGen.Emit(OpCodes.Call, metadata.HandlerMethod);
            executeHandlerGen.Emit(OpCodes.Stloc_0);

            executeHandlerGen.Emit(OpCodes.Ldarg_0);
            executeHandlerGen.Emit(OpCodes.Ldloc_0);
            executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty(nameof(RpcContext.ResponseTask))?.SetMethod!);

            executeHandlerGen.Emit(OpCodes.Ldloc_0);
            executeHandlerGen.Emit(OpCodes.Ret);

            MethodInfo lastMethod = executeHandler;
            delegateFieldMap.Add(executeHandler, typeBuilder.DefineField(
                "executeHandlerDelegateField",
                typeof(RpcRequestDelegate),
                FieldAttributes.Private | FieldAttributes.Static
            ));

            for (int i = metadata.MiddlewareTypes.Count - 1; i >= 0; i--)
            {
                var middlewareType = metadata.MiddlewareTypes[i];

                var currentMethod = typeBuilder.DefineMethod(
                    "middlewareRun" + i,
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(Task),
                    new[] {typeof(RpcContext), typeof(InstanceProvider)}
                );

                var gen = currentMethod.GetILGenerator();

                // load the middleware instance from the instanceProvider
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Ldtoken, middlewareType);
                gen.Emit(OpCodes.Callvirt, getInstance);

                // load the context
                gen.Emit(OpCodes.Ldarg_0);

                // load the instanceProvider
                gen.Emit(OpCodes.Ldarg_1);

                // load the next delegate
                gen.Emit(OpCodes.Ldsfld, delegateFieldMap[lastMethod]);

                // call the middleware RUN function
                gen.Emit(OpCodes.Call, middlewareType.GetMethod(nameof(RpcMiddleware.Run))!);

                gen.Emit(OpCodes.Ret);

                lastMethod = currentMethod;

                delegateFieldMap.Add(currentMethod, typeBuilder.DefineField(
                    "middlewareDelegateField" + i,
                    typeof(RpcRequestDelegate),
                    FieldAttributes.Private | FieldAttributes.Static
                ));
            }

            // initialize the delegate fields
            var initializeDelegateFieldsMethod = typeBuilder.DefineMethod(
                "initializeDelegateFields",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(void),
                Array.Empty<Type>()
            );

            var initializeDelegateIlGen = initializeDelegateFieldsMethod.GetILGenerator();

            foreach (var kvp in delegateFieldMap)
            {
                initializeDelegateIlGen.Emit(OpCodes.Ldnull);
                initializeDelegateIlGen.Emit(OpCodes.Ldftn, kvp.Key);
                initializeDelegateIlGen.Emit(OpCodes.Newobj, typeof(RpcRequestDelegate).GetConstructors()[0]);
                initializeDelegateIlGen.Emit(OpCodes.Stsfld, kvp.Value);
            }

            initializeDelegateIlGen.Emit(OpCodes.Ret);

            var dynamicType = typeBuilder.CreateTypeInfo()!.AsType();

            var methodInfo = dynamicType.GetMethod(lastMethod.Name, BindingFlags.NonPublic | BindingFlags.Static);

            var run = methodInfo!.CreateDelegate<RpcRequestDelegate>();

            var initializeDelegateFields = dynamicType
                    .GetMethod(initializeDelegateFieldsMethod.Name, BindingFlags.NonPublic | BindingFlags.Static)!
                .CreateDelegate<Action>();

            initializeDelegateFields();

            return run;
        }

        public async Task<Result<object>> Execute(RpcRequestMessage requestMessage, InstanceProvider instanceProvider)
        {
            if (requestMessage == null)
            {
                throw new DetailedLogException("Request message is null.");
            }

            if (requestMessage.Payload == null)
            {
                throw new DetailedLogException("Request payload is null.");
            }

            if (string.IsNullOrWhiteSpace(requestMessage.Type))
            {
                throw new DetailedLogException("Request type is null or empty.");
            }

            RpcRequestMetadata metadata;

            if (!this.metadataByRequestName.TryGetValue(requestMessage.Type, out metadata))
            {
                throw new DetailedLogException($"No RPC handler for request `{requestMessage.Type}`.");
            }

            var context = new RpcContext
            {
                HandlerParameters = new Dictionary<Type, object>(),
                RequestMetadata = metadata,
                RequestMessage = requestMessage,
            };

            await metadata.CompiledMethod(context, instanceProvider);

            if (context.ResponseTask == null)
            {
                throw new DetailedLogException("Rpc response task is null.");
            }

            return this.convertReturnValue(context.ResponseTask);
        }

        public RpcRequestMetadata GetMetadataByRequestName(string requestName)
        {
            if (this.metadataByRequestName.TryGetValue(requestName, out var metadata))
            {
                return metadata;
            }

            return null;
        }
    }

    /// <summary>
    /// Represents an RPC request in execution.
    /// </summary>
    public class RpcContext
    {
        public RpcRequestMessage RequestMessage { get; set; }

        public RpcRequestMetadata RequestMetadata { get; set; }

        // ReSharper disable once CollectionNeverQueried.Global
        public Dictionary<Type, object> HandlerParameters { get; set; }

        public Task ResponseTask { get; set; }

        public void SetResponse(Result result)
        {
            this.ResponseTask = Task.FromResult(result);
        }

        public void SetHandlerArgument<T>(T argument) where T : class
        {
            this.HandlerParameters.Add(typeof(T), argument);
        }

        public T GetSupplementalAttribute<T>() where T : RpcSupplementalAttribute
        {
            return (T) this.RequestMetadata.SupplementalAttributes.GetValueOrDefault(typeof(T));
        }
    }

    /// <summary>
    /// Base interface for all RPC middleware classes.
    /// </summary>
    public interface RpcMiddleware
    {
        Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next);
    }

    public delegate Task RpcRequestDelegate(RpcContext context, InstanceProvider instanceProvider);

    public class RpcEngineOptions
    {
        public Type[] PotentialHandlerTypes { get; set; }

        public Type[] MiddlewareTypes { get; set; }

        /// <summary>
        /// Types that are allowed to be injected into handler methods.
        /// </summary>
        public Type[] ParameterTypeWhitelist { get; set; }
    }

    public class RpcRequestMetadata
    {
        public Type DeclaringType { get; set; }

        public MethodInfo HandlerMethod { get; set; }

        public Type RequestType { get; set; }

        public Type ResponseType { get; set; }

        public List<Type> ParameterTypes { get; set; }

        public Dictionary<Type, RpcSupplementalAttribute> SupplementalAttributes { get; set; }

        public RpcRequestDelegate CompiledMethod { get; set; }

        public List<Type> MiddlewareTypes { get; set; }
    }

    /// <summary>
    /// This is used to mark methods as RPC handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    public class RpcBindAttribute : Attribute
    {
        public RpcBindAttribute(Type requestType, Type responseType)
        {
            this.RequestType = requestType;
            this.ResponseType = responseType;
        }

        public Type RequestType { get; }

        public Type ResponseType { get; }
    }

    /// <summary>
    /// This is the base type for all supplemental attributes.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, Inherited = false)]
    public abstract class RpcSupplementalAttribute : Attribute { }

    /// <summary>
    /// The input to the RPC engine.
    /// </summary>
    public class RpcRequestMessage
    {
        public object Payload { get; set; }

        public string Type { get; set; }
    }
}
