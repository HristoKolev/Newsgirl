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

                (bool isValid, var validationErrorMessages) = DataValidator.Validate(req.Payload);

                if (!isValid)
                {
                    return ApiResult.FromErrorMessages(validationErrorMessages);
                }

                var parameters = handler.Method.GetParameters()
                                        .Select(info =>
                                        {
                                            if (info.ParameterType == handler.RequestType)
                                            {
                                                return req.Payload;
                                            }

                                            throw new NotSupportedException(
                                                $"Parameters don't match for handler method {handler.Method.DeclaringType.Name}.{handler.Method.Name}");
                                        }).ToArray();

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
            catch (Exception ex)
            {
                var log = resolver.Resolve<MainLogger>();
                await log.LogError(ex);

                string message;

                #pragma warning disable 162

                // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                if (Global.Debug)
                {
                    message = ex.ToString();
                }
                else
                {
                    message = $"An error occurred while executing request with type `{req.Type}`.";
                }
                #pragma warning restore 162

                return ApiResult.FromErrorMessage(message);
            }
        }

        public static HandlerCollection ScanForHandlers(params Assembly[] assemblies)
        {
            var methods = assemblies.SelectMany(assembly => assembly.GetTypes())
                                    .SelectMany(type =>
                                                    type.GetMethods(
                                                        BindingFlags.Instance |
                                                        BindingFlags.Public |
                                                        BindingFlags.Static |
                                                        BindingFlags.NonPublic))
                                    .Where(info =>
                                               info.GetCustomAttribute<BindRequestAttribute>() !=
                                               null);

            var handlerMap = new Dictionary<Type, MethodInfo>();

            var handlers = new List<HandlerCollection.ApiHandlerModel>();

            foreach (var methodInfo in methods)
            {
                var requestType = methodInfo.GetCustomAttribute<BindRequestAttribute>().RequestType;

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

                if (handlerMap.ContainsKey(requestType))
                {
                    var existingMethodInfo = handlerMap[requestType];

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

                handlerMap.Add(requestType, methodInfo);

                var handlerTypeAttributes = methodInfo.DeclaringType.GetCustomAttributes().ToList();
                var handlerMethodAttributes = methodInfo.GetCustomAttributes().ToList();

                var allAttributes = handlerTypeAttributes.Concat(handlerMethodAttributes).ToList();

                handlers.Add(new HandlerCollection.ApiHandlerModel
                {
                    RequestType = requestType,
                    Method = methodInfo,
                    HandlerType = methodInfo.DeclaringType,
                    ExecuteInTransaction = allAttributes.OfType<InTransactionAttribute>().Any(),
                    RequireAuthentication = allAttributes.OfType<AuthenticateHandlerAttribute>().Any()
                });
            }

            return new HandlerCollection(handlers);
        }

        private static async Task<ApiResult> ExecuteHandlerMethod(
            MethodInfo methodInfo, 
            object handlerInstance,
            object[] parameters)
        {
            if (typeof(Task).IsAssignableFrom(methodInfo.ReturnType))
            {
                if (methodInfo.ReturnType == typeof(Task))
                {
                    await (Task) methodInfo.Invoke(handlerInstance, parameters);

                    return ApiResult.SuccessfulResult();
                }

                var task = (Task) methodInfo.Invoke(handlerInstance, parameters);

                await task;

                object returnValue = task.GetType().GetProperty("Result").GetValue(task);

                if (returnValue is ApiResult result)
                {
                    return result;
                }

                return ApiResult.SuccessfulResult(returnValue);
            }
            else
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

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class InTransactionAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Method)]
    public class BindRequestAttribute : Attribute
    {
        public BindRequestAttribute(Type requestType)
        {
            this.RequestType = requestType;
        }

        public Type RequestType { get; }
    }

    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class AuthenticateHandlerAttribute : Attribute
    {
    }

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