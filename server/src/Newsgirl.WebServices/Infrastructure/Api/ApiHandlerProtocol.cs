namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using Data;

    using Npgsql;

    public static class ApiHandlerProtocol
    {
        public static async Task<ApiResult> ProcessRequest(ApiRequest req, HandlerCollection handlers, TypeResolver resolver)
        {
            try
            {
                var handler = handlers.GetHandler(req.Type);
                var handlerInstance = resolver.Resolve(handler.Method.DeclaringType);
                
                // Request payload validation.
                var validationResult = DataValidator.Validate(req.Payload);
                if (!validationResult.IsSuccess)
                {
                    return ApiResult.FromErrorMessages(validationResult.ErrorMessages);
                }

                // Resolve the parameters.
                var parameters = handler.Method.GetParameters().Select(info =>
                {
                    if (info.ParameterType == handler.RequestType)
                    {
                        return req.Payload;
                    }

                    string message = $"Parameters don't match for handler method {handler.Method.DeclaringType.Name}.{handler.Method.Name}";

                    throw new NotSupportedException(message);
                }).ToArray();

                // In case of a transaction.
                // Call the handler's method,
                // if it throws - rollback,
                // if it runs and produces a failed response - rollback,
                // if it returns a successful response - commit.
                if (handler.ExecuteInTransaction)
                {
                    var db = resolver.Resolve<IDbService>();

                    NpgsqlTransaction tx = null;

                    try
                    {
                        tx = await db.BeginTransaction();

                        var result = await ExecuteHandlerMethod(handler.Method, handlerInstance, parameters);

                        if (!result.Success)
                        {
                            await tx.RollbackAsync();

                            return result;
                        }

                        await tx.CommitAsync();

                        return result;
                    }
                    catch (Exception)
                    {
                        if (tx != null)
                        {
                            await tx.RollbackAsync();
                        }

                        throw;
                    }
                }

                return await ExecuteHandlerMethod(handler.Method, handlerInstance, parameters);
            }
            catch (Exception ex) // In case of an error.
            {
                // Log the error.
                var log = resolver.Resolve<MainLogger>();
                await log.LogError(ex);
                
                // In a debug environment - return the stack trace as an error message.
                // In release - just return an acknowledgment that an error has occured.
                string message;
                
                if (Global.Debug)
                {
                    message = ex.ToString();
                }
                else
                {
                    message = $"An error occurred while executing request with type `{req.Type}`.";
                }

                return ApiResult.FromErrorMessage(message);
            }
        }

        public static HandlerCollection ScanForHandlers(params Assembly[] assemblies)
        {
            var allMethodsFilter = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            
            // Gets all methods that have `BindRequestAttribute` of all types in the provided assemblies.
            var methods = assemblies.SelectMany(assembly => assembly.GetTypes())
                                    .SelectMany(type => type.GetMethods(allMethodsFilter))
                                    .Where(info => info.GetCustomAttribute<BindRequestAttribute>() != null)
                                    .ToList();
            
            var requestTypeByHandlerMethodInfo = new Dictionary<Type, MethodInfo>();

            var handlers = new List<HandlerCollection.ApiHandlerModel>();

            foreach (var methodInfo in methods)
            {
                var bindRequestAttribute = methodInfo.GetCustomAttribute<BindRequestAttribute>();
                
                var requestType = bindRequestAttribute.RequestType;

                if (!methodInfo.IsPublic)
                {
                    throw new NotSupportedException(
                        string.Format(
                            "Handler binding error. The method {0} {1}.{2} is not Public.",

                            // ReSharper disable once PossibleNullReferenceException
                            requestType.Name,
                            methodInfo.DeclaringType.Name,
                            methodInfo.Name));
                }

                if (methodInfo.IsStatic)
                {
                    throw new NotSupportedException(
                        string.Format(
                            "Handler binding error. The method {0} {1}.{2} is Static.",

                            // ReSharper disable once PossibleNullReferenceException
                            requestType.Name,
                            methodInfo.DeclaringType.Name,
                            methodInfo.Name));
                }

                foreach (var parameterInfo in methodInfo.GetParameters())
                {
                    if (parameterInfo.ParameterType != requestType && parameterInfo.ParameterType != typeof(ApiRequest))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        throw new NotSupportedException(
                            string.Format(
                                "Parameters don't match for handler method {0}.{1}",
                                methodInfo.DeclaringType.Name,
                                methodInfo.Name));
                    }
                }

                if (requestTypeByHandlerMethodInfo.ContainsKey(requestType))
                {
                    var existingMethodInfo = requestTypeByHandlerMethodInfo[requestType];

                    throw new NotSupportedException(
                        "Handler binding conflict. 2 request types are bound to the same handler method." +

                        // ReSharper disable once PossibleNullReferenceException
                        $"{requestType.Name} => {existingMethodInfo.DeclaringType.Name}.{existingMethodInfo.Name}" +

                        // ReSharper disable once PossibleNullReferenceException
                        $"{requestType.Name} => {methodInfo.DeclaringType.Name}.{methodInfo.Name}");
                }

                var returnType = methodInfo.ReturnType;

                if (typeof(Task).IsAssignableFrom(returnType))
                {
                    if (returnType != typeof(Task) && returnType.GetGenericTypeDefinition() != typeof(Task<>))
                    {
                        throw new NotSupportedException(
                            "Invalid method return type. If the method is async only Task and Task<T> return types allowed." +
                            $"{requestType.Name} => {methodInfo.ReturnType.Name} {methodInfo.DeclaringType.Name}.{methodInfo.Name}");
                    }
                }

                requestTypeByHandlerMethodInfo.Add(requestType, methodInfo);

                var handlerTypeAttributes = methodInfo.DeclaringType.GetCustomAttributes().ToList();
                var handlerMethodAttributes = methodInfo.GetCustomAttributes().ToList();

                var allAttributes = handlerTypeAttributes.Concat(handlerMethodAttributes).ToList();

                handlers.Add(new HandlerCollection.ApiHandlerModel
                {
                    RequestType = requestType,
                    ResponseType = bindRequestAttribute.ResponseType,
                    Method = methodInfo,
                    HandlerType = methodInfo.DeclaringType,
                    ExecuteInTransaction = allAttributes.OfType<InTransactionAttribute>().Any(),
                    RequireAuthentication = allAttributes.OfType<AuthenticateHandlerAttribute>().Any()
                });
            }

            return new HandlerCollection(handlers);
        }

        /// <summary>
        /// An abstraction around calling the handler's method.
        /// The idea is that it may return (void or an object) or Task (of something)
        /// and it should be handled accordingly.
        /// If the return value is `ApiResult` then return it, else - wrap it in a successful `ApiResult`. 
        /// </summary>
        private static async Task<ApiResult> ExecuteHandlerMethod(
            MethodInfo methodInfo, 
            object handlerInstance,
            object[] parameters)
        {
            // If it's an awaitable.
            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                // If it's a simple task - just await it and if it does not throw - return success. 
                if (methodInfo.ReturnType == typeof(Task))
                {
                    await (Task) methodInfo.Invoke(handlerInstance, parameters);
                    return ApiResult.SuccessfulResult();
                }

                // If it's a Task<T>, await the task and then get the Result with reflection.
                var task = (Task) methodInfo.Invoke(handlerInstance, parameters);
                await task;
                object returnValue = task.GetType().GetProperty("Result").GetValue(task);

                // Wrap if needed.
                if (returnValue is ApiResult result)
                {
                    return result;
                }

                return ApiResult.SuccessfulResult(returnValue);
            }
            else // Just a normal value.
            {
                if (methodInfo.ReturnType == typeof(void))
                {
                    methodInfo.Invoke(handlerInstance, parameters);
                    return ApiResult.SuccessfulResult();
                }

                var returnValue = methodInfo.Invoke(handlerInstance, parameters);

                if (returnValue is ApiResult result)
                {
                    return result;
                }

                return ApiResult.SuccessfulResult(returnValue);
            }
        }
    }

    /// <summary>
    /// Put this on a handler method in order to run it in a transaction.
    /// You can also put it on a handler class,
    /// that way every handler method wil run in a transaction.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class InTransactionAttribute : Attribute
    {
    }

    /// <summary>
    /// Put this on a method to mark it as a handler method.
    /// You have to specify the request Dto type. 
    /// </summary>
    [AttributeUsage(AttributeTargets.Method)]
    public class BindRequestAttribute : Attribute
    {
        public BindRequestAttribute(Type requestType, Type responseType)
        {
            this.RequestType = requestType;
            this.ResponseType = responseType;
        }
        
        public BindRequestAttribute(Type requestType)
        {
            this.RequestType = requestType;
        }

        public Type RequestType { get; }

        public Type ResponseType { get; }
    }

    /// <summary>
    /// Use this in order to specify that the handler method have to be called by an authenticated user.
    /// This is used for handler methods that are designed to be called from HTTP.
    /// Calls coming from the `DirectApiClient` will aways be executed.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AuthenticateHandlerAttribute : Attribute
    {
    }

    /// <summary>
    /// A master type that holds handler metadata.
    /// </summary>
    public class HandlerCollection
    {
        public HandlerCollection(IReadOnlyCollection<ApiHandlerModel> handlers)
        {
            this.Handlers = handlers;
            this.HandlersByMessageType = handlers.ToDictionary(model => model.RequestType.Name, model => model);
        }

        private IReadOnlyCollection<ApiHandlerModel> Handlers { get; }

        private IDictionary<string, ApiHandlerModel> HandlersByMessageType { get; }

        public IEnumerable<ApiHandlerModel> GetAllHandlers()
        {
            return this.Handlers.ToList();
        }

        public ApiHandlerModel GetHandler(string messageType)
        {
            if (!this.HandlersByMessageType.ContainsKey(messageType))
            {
                return null;
            }

            return this.HandlersByMessageType[messageType];
        }

        public class ApiHandlerModel
        {
            public bool ExecuteInTransaction { get; set; }

            public Type HandlerType { get; set; }

            public MethodInfo Method { get; set; }

            public Type RequestType { get; set; }

            public bool RequireAuthentication { get; set; }

            public Type ResponseType { get; set; }
        }
    }

    public class ApiResult
    {
        public bool Success { get; set; }

        public string[] ErrorMessages { get; set; }

        public object Payload { get; set; }

        public static ApiResult SuccessfulResult()
        {
            return new ApiResult
            {
                Success = true
            };
        }

        public static ApiResult SuccessfulResult(object payload)
        {
            return new ApiResult
            {
                Success = true,
                Payload = payload
            };
        }

        public static ApiResult FromErrorMessage(string message)
        {
            return new ApiResult
            {
                Success = false,
                ErrorMessages = new[] {message}
            };
        }

        public static ApiResult FromErrorMessages(string[] errorMessages)
        {
            return new ApiResult
            {
                Success = false,
                ErrorMessages = errorMessages
            };
        }
    }

    public class ApiRequest
    {
        public object Payload { get; set; }

        public string Type { get; set; }
    }
}