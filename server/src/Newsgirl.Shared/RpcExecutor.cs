using System.Linq;
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

        public async Task Execute(string requestName, object requestPayload)
        {
            if (string.IsNullOrWhiteSpace(requestName))
            {
                throw new DetailedLogException("Request name is null or white space.");
            }

            if (requestPayload == null)
            {
                throw new DetailedLogException("Request payload is null.");
            }

            var metadata = this.handlerCollection.GetMetadataByRequestName(requestName);

            if (metadata == null)
            {
                throw new DetailedLogException($"No RPC handler for request `{requestName}`.");
            }

            var handlerInstance = this.resolver.Resolve(metadata.HandlerClass);

            var parameters = new[]
            {
                requestPayload
            };

            var returnValue = metadata.HandlerMethod.Invoke(handlerInstance, parameters);
        }
    }
}
