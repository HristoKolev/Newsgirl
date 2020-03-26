using System;
using System.Threading.Tasks;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcExecutor
    {
        private readonly RpcMetadataCollection handlerCollection;
        private readonly InstanceProvider resolver;

        public RpcExecutor(RpcMetadataCollection handlerCollection, InstanceProvider resolver)
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

            var handlerInstance = this.resolver.Get(metadata.HandlerClass);

            object returnValue = metadata.CompiledHandler(handlerInstance, requestPayload, new FakeInstanceProviders());

            var response = await (Task<TResponse>)returnValue;

            return response;
        }
    }
    
    public class FakeInstanceProviders: InstanceProvider {
        
        public object Get(Type type)
        {
            throw new ApplicationException($"GET_INSTANCE {type.Name}");
        }
    }
}
