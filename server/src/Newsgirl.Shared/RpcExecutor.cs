using System.Threading.Tasks;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcExecutor
    {
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
        }
    }
}
