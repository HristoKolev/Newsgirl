using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcEngine
    {
        private Dictionary<string, RpcMetadata> metadataByRequestName;
        private readonly ILog log;
        
        public List<RpcMetadata> Metadata { get; private set; }

        public RpcEngine(RpcEngineOptions options, ILog log)
        {
            this.log = log;
            this.Build(options);
        }
        
        private void Build(RpcEngineOptions options)
        {
            var methodFlag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            
            var markedMethods = options.PotentialHandlerTypes.SelectMany(type => type.GetMethods(methodFlag))
                .Where(info => info.GetCustomAttribute<RpcBindAttribute>() != null).ToList();

            var handlers = new List<RpcMetadata>(markedMethods.Count);

            foreach (var markedMethod in markedMethods)
            {
                if (markedMethod.IsStatic)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    throw new DetailedLogException($"Static methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                }
                
                if (markedMethod.IsPrivate)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    throw new DetailedLogException($"Private methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                }

                var bindAttribute = markedMethod.GetCustomAttribute<RpcBindAttribute>();

                var metadata = new RpcMetadata
                {
                    HandlerClass = markedMethod.DeclaringType,
                    HandlerMethod = markedMethod,
                    RequestType = bindAttribute.RequestType,
                    ResponseType = bindAttribute.ResponseType,
                    Parameters = markedMethod.GetParameters().Select(x => x.ParameterType).ToList(),
                    SupplementalAttributes = new Dictionary<Type, RpcSupplementalAttribute>(),
                };

                // ReSharper disable once AssignNullToNotNullAttribute
                var allSupplementalAttributes = metadata.HandlerClass.GetCustomAttributes()
                    .Concat(metadata.HandlerMethod.GetCustomAttributes())
                    .Where(x => x is RpcSupplementalAttribute)
                    .Cast<RpcSupplementalAttribute>()
                    .ToList();
                
                foreach (var supplementalAttribute in allSupplementalAttributes)
                {
                    var type = supplementalAttribute.GetType();
                    metadata.SupplementalAttributes[type] = supplementalAttribute;
                }

                var allowedParameterTypes = new List<Type> {metadata.RequestType};

                if (options.HandlerArgumentTypeWhiteList != null)
                {
                    allowedParameterTypes.AddRange(options.HandlerArgumentTypeWhiteList);
                }

                foreach (var parameter in metadata.Parameters)
                {
                    if (!allowedParameterTypes.Contains(parameter))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        throw new DetailedLogException($"Parameter of type {parameter.Name} is not supported for RPC methods. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                    }
                }

                var taskWrappedResponseType = typeof(Task<>).MakeGenericType(metadata.ResponseType);

                var taskAndResultWrappedResponseType = typeof(Task<>).MakeGenericType(typeof(RpcResult<>).MakeGenericType(metadata.ResponseType));

                if (metadata.HandlerMethod.ReturnType == taskAndResultWrappedResponseType)
                {
                    metadata.DefaultReturnVariant = ReturnVariant.TaskOfResultOfResponse;
                }
                else if (metadata.HandlerMethod.ReturnType == taskWrappedResponseType)
                {
                    metadata.DefaultReturnVariant = ReturnVariant.TaskOfResponse;
                }
                else
                {
                    throw new DetailedLogException(
                        $"Only Task<{metadata.ResponseType}> and Task<Result<{metadata.ResponseType}>> are supported for" +
                        $" handler method `{metadata.HandlerClass.Name}.{metadata.HandlerMethod.Name}`.");
                }

                var collidingMetadata = handlers.FirstOrDefault(x => x.RequestType == metadata.RequestType);

                if (collidingMetadata != null)
                {
                    throw new DetailedLogException(
                        "Handler binding conflict. A request is bound to 2 or more handler methods." +

                        $"{metadata.RequestType.Name} => {collidingMetadata.HandlerClass.Name}.{collidingMetadata.HandlerMethod.Name} AND " +

                        $"{metadata.RequestType.Name} => {metadata.HandlerClass.Name}.{metadata.HandlerMethod.Name}");
                }

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
                            throw new DetailedLogException($"Middleware type {middlewareType.Name} does not implement {nameof(RpcMiddleware)}.");
                        }
                    }

                    metadata.MiddlewareTypes = options.MiddlewareTypes.ToList();
                }

                metadata.CompiledMethod = CompileHandlerWithMiddleware(metadata);
                
                metadata.ConvertTaskOfResponse = GetConvertTaskOfResponse(metadata);
                metadata.ConvertTaskOfResultOfResponse = GetConvertTaskOfResultOfResponse(metadata);
                metadata.ConvertTaskOfResult = GetConvertTaskOfResult(metadata);

                handlers.Add(metadata);
            }

            this.Metadata = handlers.OrderBy(x => x.RequestType.Name).ToList();
            this.metadataByRequestName = handlers.ToDictionary(x => x.RequestType.Name, x => x);
        }

        // ReSharper disable once UnusedParameter.Local
        private static Func<Task, RpcResult<object>> GetConvertTaskOfResult(RpcMetadata metadata)
        {
            var method = new DynamicMethod("convertTaskOfResult", typeof(RpcResult<object>), new []{typeof(Task)});
            
            var il = method.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(Task<RpcResult>).GetProperty("Result")?.GetMethod);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcResult).GetProperty("ErrorMessages")?.GetMethod);
            il.Emit(OpCodes.Call, typeof(RpcResult).GetMethods().First(x => x.Name == "Error" && x.IsGenericMethod && x.GetParameters().First().ParameterType == typeof(string[])).MakeGenericMethod(typeof(object)));
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(Task<RpcResult>).GetProperty("Result")?.GetMethod);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcResult).GetProperty("Headers")?.GetMethod);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcResult).GetProperty("Headers")?.SetMethod);
            
            il.Emit(OpCodes.Ret);
            
            return method.CreateDelegate<Func<Task, RpcResult<object>>>();
        }

        private static Func<Task, RpcResult<object>> GetConvertTaskOfResultOfResponse(RpcMetadata metadata)
        {
            var method = new DynamicMethod("convertTaskOfResultOfResponse", typeof(RpcResult<object>), new []{typeof(Task)});

            var il = method.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(Task<>).MakeGenericType(typeof(RpcResult<>).MakeGenericType(metadata.ResponseType)).GetProperty("Result")?.GetMethod);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcResult<>).MakeGenericType(metadata.ResponseType).GetProperty("Payload")?.GetMethod);
            il.Emit(OpCodes.Call, typeof(RpcResult).GetMethods().First(x => x.Name == "Ok" && x.IsGenericMethod && x.GetParameters().Length == 1).MakeGenericMethod(typeof(object)));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate<Func<Task, RpcResult<object>>>();
        }

        private static Func<Task, RpcResult<object>> GetConvertTaskOfResponse(RpcMetadata metadata)
        {
            var method = new DynamicMethod("convertTaskOfResponse", typeof(RpcResult<object>), new []{typeof(Task)});

            var il = method.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(Task<>).MakeGenericType(metadata.ResponseType).GetProperty("Result")?.GetMethod);
            il.Emit(OpCodes.Call, typeof(RpcResult).GetMethods().First(x => x.Name == "Ok" && x.IsGenericMethod && x.GetParameters().Length == 1).MakeGenericMethod(typeof(object)));
            il.Emit(OpCodes.Ret);

            return method.CreateDelegate<Func<Task, RpcResult<object>>>();
        }

        private static RpcRequestDelegate CompileHandlerWithMiddleware(RpcMetadata metadata)
        {
            var getInstance = typeof(InstanceProvider).GetMethod("Get", new[] {typeof(Type)});

            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType(
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
            executeHandlerGen.Emit(OpCodes.Ldtoken, metadata.HandlerClass);
            // ReSharper disable once AssignNullToNotNullAttribute
            executeHandlerGen.Emit(OpCodes.Callvirt, getInstance);

            foreach (var parameter in metadata.Parameters)
            {
                if (parameter == metadata.RequestType)
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("RequestMessage")?.GetMethod);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty("Payload")?.GetMethod);
                }
                else
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("Items")?.GetMethod);
                    executeHandlerGen.Emit(OpCodes.Ldtoken, parameter);
                    // ReSharper disable once AssignNullToNotNullAttribute
                    executeHandlerGen.Emit(OpCodes.Call, typeof(Dictionary<Type, object>).GetMethod("get_Item"));
                }
            }
                    
            executeHandlerGen.Emit(OpCodes.Call, metadata.HandlerMethod);
            executeHandlerGen.Emit(OpCodes.Stloc_0);
            
            executeHandlerGen.Emit(OpCodes.Ldarg_0);
            executeHandlerGen.Emit(OpCodes.Ldloc_0);
            // ReSharper disable once AssignNullToNotNullAttribute
            executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("ResponseTask")?.SetMethod);
                    
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
                // ReSharper disable once AssignNullToNotNullAttribute
                gen.Emit(OpCodes.Call, middlewareType.GetMethod("Run"));
                
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
            
            // ReSharper disable once PossibleNullReferenceException
            var dynamicType = typeBuilder.CreateTypeInfo().AsType();

            var methodInfo = dynamicType.GetMethod(lastMethod.Name, BindingFlags.NonPublic | BindingFlags.Static);
            
            var run = methodInfo.CreateDelegate<RpcRequestDelegate>();

            var initializeDelegateFields = dynamicType.GetMethod(
                initializeDelegateFieldsMethod.Name, 
                BindingFlags.NonPublic | BindingFlags.Static
            )
                .CreateDelegate<Action>();

            initializeDelegateFields();

            return run;
        }
        
        public async Task<RpcResult<object>> Execute(RpcRequestMessage requestMessage, InstanceProvider instanceProvider)
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

            // ReSharper disable once InlineOutVariableDeclaration
            RpcMetadata metadata;

            if (!this.metadataByRequestName.TryGetValue(requestMessage.Type, out metadata))
            {
                throw new DetailedLogException($"No RPC handler for request `{requestMessage.Type}`.");
            }

            var context = new RpcContext
            {
                Items = new Dictionary<Type, object>(),
                Metadata = metadata,
                ReturnVariant = metadata.DefaultReturnVariant,
                RequestMessage = requestMessage,
            };

            try
            {
                await metadata.CompiledMethod(context, instanceProvider);
            }
            catch (Exception err)
            {
                await this.log.Error(err);

                return RpcResult.Error<object>($"{err.GetType().Name}: {err.Message}");
            }

            switch (context.ReturnVariant)
            {
                case ReturnVariant.TaskOfResponse:
                {
                    return metadata.ConvertTaskOfResponse(context.ResponseTask);
                }
                case ReturnVariant.TaskOfResultOfResponse:
                {
                    return metadata.ConvertTaskOfResultOfResponse(context.ResponseTask);
                }
                case ReturnVariant.TaskOfResult:
                {
                    return metadata.ConvertTaskOfResult(context.ResponseTask);
                }
                default:
                {
                    throw new DetailedLogException($"unknown ReturnVariant: {(int)context.ReturnVariant}.");
                }
            }
        }

        public async Task<RpcResult<TResponse>> Execute<TResponse>(RpcRequestMessage requestMessage, InstanceProvider instanceProvider)
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

            // ReSharper disable once InlineOutVariableDeclaration
            RpcMetadata metadata;

            if (!this.metadataByRequestName.TryGetValue(requestMessage.Type, out metadata))
            {
                throw new DetailedLogException($"No RPC handler for request `{requestMessage.Type}`.");
            }

            var context = new RpcContext
            {
                Items = new Dictionary<Type, object>(),
                Metadata = metadata,
                ReturnVariant = metadata.DefaultReturnVariant,
                RequestMessage = requestMessage,
            };

            try
            {
                await metadata.CompiledMethod(context, instanceProvider);
            }
            catch (Exception err)
            {
                await this.log.Error(err);

                return RpcResult.Error<TResponse>($"{err.GetType().Name}: {err.Message}");
            }

            switch (context.ResponseTask)
            {
                case Task<TResponse> taskOfResponse:
                {
                    return RpcResult.Ok(taskOfResponse.Result);
                }
                case Task<RpcResult<TResponse>> taskOfResultOfResponse:
                {
                    return taskOfResultOfResponse.Result;
                }
                case Task<RpcResult> taskOfResult:
                {
                    return new RpcResult<TResponse>
                    {
                        ErrorMessages = taskOfResult.Result.ErrorMessages,
                        Headers = taskOfResult.Result.Headers,
                    };
                }
                case null:
                {
                    return RpcResult.Error<TResponse>("Rpc response task is null.");
                }
                default:
                {
                    return RpcResult.Error<TResponse>("Rpc response task type is not supported.");
                }
            }
        }

        public RpcMetadata GetMetadataByRequestName(string requestName)
        {
            if (this.metadataByRequestName.TryGetValue(requestName, out var metadata))
            {
                return metadata;
            }

            return null;
        }
    }
    
    public static class IlGeneratorHelper
    {
        private static bool initialized;
        private static readonly object SyncRoot = new object();
        private static ModuleBuilder moduleBuilder;

        private static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                var assemblyName = new AssemblyName("DynamicAssembly+" + Guid.NewGuid());
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

                initialized = true;
            }
        }

        public static ModuleBuilder ModuleBuilder
        {
            get
            {
                Initialize();
                return moduleBuilder;
            }
        }

        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
        {
            return (T) methodInfo.CreateDelegate(typeof(T));
        }
    }
    
    public class RpcContext
    {
        private Task responseTask;
        
        public Dictionary<Type, object> Items { get; set; }

        public Task ResponseTask
        {
            get => this.responseTask;
            set => this.responseTask = value;
        }

        public RpcMetadata Metadata { get; set; }
        
        public ReturnVariant ReturnVariant { get; set; }
        
        public RpcRequestMessage RequestMessage { get; set; }

        public void SetResponse<T>(T result)
        {
            this.responseTask = Task.FromResult(result);
        }
    }
    
    public interface RpcMiddleware
    {
        Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next);
    }

    public delegate Task RpcRequestDelegate(RpcContext context, InstanceProvider instanceProvider);
    
    public class RpcEngineOptions
    {
        public Type[] PotentialHandlerTypes { get; set; }

        public Type[] MiddlewareTypes { get; set; }
        
        public Type[] HandlerArgumentTypeWhiteList { get; set; }
    }

    public enum ReturnVariant
    {
        // Task<TResponse>
        TaskOfResponse = 1,
        
        // Task<RpcResult<TResponse>>
        TaskOfResultOfResponse = 2,
        
        // Task<RpcResult>
        TaskOfResult = 3,
    }

    public class RpcMetadata
    {
        public Type HandlerClass { get; set; }

        public MethodInfo HandlerMethod { get; set; }
        
        public Type RequestType { get; set; }
        
        public Type ResponseType { get; set; }

        public List<Type> Parameters { get; set; }
        
        public Dictionary<Type, RpcSupplementalAttribute> SupplementalAttributes { get; set; }

        public RpcRequestDelegate CompiledMethod { get; set; }
        
        public List<Type> MiddlewareTypes { get; set; }
        
        public ReturnVariant DefaultReturnVariant { get; set; }
        
        public Func<Task, RpcResult<object>> ConvertTaskOfResponse { get; set; }
        
        public Func<Task, RpcResult<object>> ConvertTaskOfResultOfResponse { get; set; }
        
        public Func<Task, RpcResult<object>> ConvertTaskOfResult { get; set; }
    }
    
    /// <summary>
    /// This is used to mark methods as RPC handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RpcBindAttribute : Attribute
    {
        public Type RequestType { get; }
        
        public Type ResponseType { get; }

        public RpcBindAttribute(Type requestType, Type responseType)
        {
            this.RequestType = requestType;
            this.ResponseType = responseType;
        }
    }
    
    /// <summary>
    /// This is the base type for all supplemental attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class RpcSupplementalAttribute : Attribute
    {
    }

    public class RpcRequestMessage
    {
        public object Payload { get; set; }

        public Dictionary<string, string> Headers { get; set; }
        
        public string Type { get; set; }
    }

    /// <summary>
    ///     Simple result type, uses generic T for the value and string[] for the errors.
    ///     Defines a bunch of constructor methods for convenience.
    /// </summary>
    public class RpcResult
    {
        public bool IsOk => this.ErrorMessages == null || this.ErrorMessages.Length == 0;
        
        public Dictionary<string, string> Headers { get; set; } = new Dictionary<string, string>(0);

        public string[] ErrorMessages { get; set; }

        public static RpcResult Ok()
        {
            return new RpcResult();
        }

        public static RpcResult<T> Ok<T>(T payload)
        {
            return new RpcResult<T> {Payload = payload};
        }

        public static RpcResult<T> Ok<T>()
        {
            return new RpcResult<T> {Payload = default};
        }

        public static RpcResult<T> Error<T>(string message)
        {
            return new RpcResult<T> {ErrorMessages = new[] {message}};
        }

        public static RpcResult Error<T>(string[] errorMessages)
        {
            return new RpcResult<T> {ErrorMessages = errorMessages};
        }

        public static RpcResult Error(string message)
        {
            return new RpcResult {ErrorMessages = new[] {message}};
        }

        public static RpcResult Error(string[] errorMessages)
        {
            return new RpcResult {ErrorMessages = errorMessages};
        }
    }

    public class RpcResult<T> : RpcResult
    {
        public T Payload { get; set; }
    }
}
