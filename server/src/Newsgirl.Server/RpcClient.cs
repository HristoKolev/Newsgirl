namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using Shared;

    public abstract class RpcClient
    {
        protected abstract Task<RpcResult<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request);

        public Task<RpcResult<PingResponse>> Ping(PingRequest request)
        {
            return this.RpcExecute<PingRequest, PingResponse>(request);
        }
    }
}
