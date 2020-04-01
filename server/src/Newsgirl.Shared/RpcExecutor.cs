using System;
using System.Collections.Generic;
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

            var context = new RpcContext
            {
                Items = new Dictionary<Type, object>(),
                Metadata = metadata,
                Request = requestPayload,
            };

            await metadata.CompiledMethod(context, this.resolver);

            var response = ((Task<TResponse>)context.ResponseTask).Result;

            return response;
        }
    }
}
