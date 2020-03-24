using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcExecutor
    {
        private readonly RpcMetadataCollection handlerCollection;
        private readonly IoCResolver resolver;

        public RpcExecutor(RpcMetadataCollection handlerCollection, IoCResolver resolver)
        {
            this.handlerCollection = handlerCollection;
            this.resolver = resolver;
        }

        public async Task<TResponse> Execute<TResponse>(object requestPayload)
        {
            if (requestPayload == null)
            {
                throw new DetailedLogException("Request payload is null.");
            }

            var requestType = requestPayload.GetType();
            
            var metadata = this.handlerCollection.GetMetadataByRequestType(requestType);

            if (metadata == null)
            {
                throw new DetailedLogException($"No RPC handler for request `{requestType.Name}`.");
            }

            var handlerInstance = this.resolver.Resolve(metadata.HandlerClass);

            var parameters = new[]
            {
                requestPayload
            };

            object returnValue;
            
            try
            {
                returnValue = metadata.HandlerMethod.Invoke(handlerInstance, parameters);
            }
            catch (Exception exception)
            {
                ExceptionDispatchInfo.Capture(exception.InnerException).Throw();
                throw new NotImplementedException();
            }

            var response = await (Task<TResponse>)returnValue;

            return response;
        }
    }
}
