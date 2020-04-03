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
        private Dictionary<Type, RpcMetadata> metadataByRequestType;
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

                var resultAndTaskWrappedResponseType = typeof(Task<>).MakeGenericType(typeof(Result<>).MakeGenericType(metadata.ResponseType));

                if (metadata.HandlerMethod.ReturnType == resultAndTaskWrappedResponseType)
                {
                    metadata.ReturnTypeIsResultWrapped = true;
                }
                else if (metadata.HandlerMethod.ReturnType != taskWrappedResponseType)
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

                metadata.CompiledMethod = CompileWithMiddleware(metadata);

                handlers.Add(metadata);
            }

            this.Metadata = handlers.OrderBy(x => x.RequestType.Name).ToList();
            this.metadataByRequestType = handlers.ToDictionary(x => x.RequestType, x => x);
        }
        
        private static RpcRequestDelegate CompileWithMiddleware(RpcMetadata metadata)
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
            executeHandlerGen.Emit(OpCodes.Callvirt, getInstance);

            foreach (var parameter in metadata.Parameters)
            {
                if (parameter == metadata.RequestType)
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("Request")?.GetMethod);
                }
                else
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("Items")?.GetMethod);
                    executeHandlerGen.Emit(OpCodes.Ldtoken, parameter);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(Dictionary<Type, object>).GetMethod("get_Item"));
                }
            }
                    
            executeHandlerGen.Emit(OpCodes.Call, metadata.HandlerMethod);
            executeHandlerGen.Emit(OpCodes.Stloc_0);
            
            executeHandlerGen.Emit(OpCodes.Ldarg_0);
            executeHandlerGen.Emit(OpCodes.Ldloc_0);
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
        
        public async Task<Result<object>> Execute(object request, InstanceProvider instanceProvider)
        {
            if (request == null)
            {
                throw new DetailedLogException("Request payload is null.");
            }

            var requestType = request.GetType();

            // ReSharper disable once InlineOutVariableDeclaration
            RpcMetadata metadata;

            if (!this.metadataByRequestType.TryGetValue(requestType, out metadata))
            {
                throw new DetailedLogException($"No RPC handler for request `{requestType.Name}`.");
            }

            var context = new RpcContext
            {
                Items = new Dictionary<Type, object>(),
                Metadata = metadata,
                Request = request,
            };

            try
            {
                await metadata.CompiledMethod(context, instanceProvider);
            }
            catch (Exception err)
            {
                await this.log.Error(err);
                
                return Result.Error<object>($"{err.GetType().Name}: {err.Message}");
            }

            switch (context.ResponseTask)
            {
                case Task<object> taskOfResponse:
                {
                    return Result.Ok(taskOfResponse.Result);
                }
                case Task<Result<object>> taskOfResultOfResponse:
                {
                    return taskOfResultOfResponse.Result;
                }
                case Task<Result> taskOfResult:
                {
                    return new Result<object>
                    {
                        ErrorMessages = taskOfResult.Result.ErrorMessages
                    };
                }
                case null:
                {
                    return Result.Error<object>("Rpc response task is null.");
                }
                default:
                {
                    return Result.Error<object>("Rpc response task type is not supported.");
                }
            }
        }

        public async Task<Result<TResponse>> Execute<TResponse>(object request, InstanceProvider instanceProvider)
        {
            if (request == null)
            {
                throw new DetailedLogException("Request payload is null.");
            }

            var requestType = request.GetType();

            // ReSharper disable once InlineOutVariableDeclaration
            RpcMetadata metadata;

            if (!this.metadataByRequestType.TryGetValue(requestType, out metadata))
            {
                throw new DetailedLogException($"No RPC handler for request `{requestType.Name}`.");
            }

            var context = new RpcContext
            {
                Items = new Dictionary<Type, object>(),
                Metadata = metadata,
                Request = request,
            };

            try
            {
                await metadata.CompiledMethod(context, instanceProvider);
            }
            catch (Exception err)
            {
                await this.log.Error(err);
                
                return Result.Error<TResponse>($"{err.GetType().Name}: {err.Message}");
            }

            switch (context.ResponseTask)
            {
                case Task<TResponse> taskOfResponse:
                {
                    return Result.Ok(taskOfResponse.Result);
                }
                case Task<Result<TResponse>> taskOfResultOfResponse:
                {
                    return taskOfResultOfResponse.Result;
                }
                case Task<Result> taskOfResult:
                {
                    return new Result<TResponse>
                    {
                        ErrorMessages = taskOfResult.Result.ErrorMessages
                    };
                }
                case null:
                {
                    return Result.Error<TResponse>("Rpc response task is null.");
                }
                default:
                {
                    return Result.Error<TResponse>("Rpc response task type is not supported.");
                }
            }
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
        
        public object Request { get; set; }

        public Dictionary<Type, object> Items { get; set; }

        public Task ResponseTask
        {
            get => this.responseTask;
            set => this.responseTask = value;
        }

        public RpcMetadata Metadata { get; set; }
        

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
        
        public bool ReturnTypeIsResultWrapped { get; set; }
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
}