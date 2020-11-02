namespace Newsgirl.Server
{
    using System.Threading.Tasks;
    using Shared;

    public abstract class RpcClient
    {
        protected abstract Task<RpcResult<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request);

        public virtual Task<RpcResult<LoginResponse>> Login(LoginRequest request)
        {
            return this.RpcExecute<LoginRequest, LoginResponse>(request);
        }

        public virtual Task<RpcResult<PingResponse>> Ping(PingRequest request)
        {
            return this.RpcExecute<PingRequest, PingResponse>(request);
        }

        public virtual Task<RpcResult<ProfileInfoResponse>> ProfileInfo(ProfileInfoRequest request)
        {
            return this.RpcExecute<ProfileInfoRequest, ProfileInfoResponse>(request);
        }

        public virtual Task<RpcResult<RegisterResponse>> Register(RegisterRequest request)
        {
            return this.RpcExecute<RegisterRequest, RegisterResponse>(request);
        }
    }
}
