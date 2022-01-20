namespace Newsgirl.Server;

using System.Threading.Tasks;
using Auth;
using Xdxd.DotNet.Shared;

public abstract class RpcClient
{
    protected abstract Task<Result<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request)
        where TRequest : class where TResponse : class;

    public virtual Task<Result<LoginResponse>> Login(LoginRequest request)
    {
        return this.RpcExecute<LoginRequest, LoginResponse>(request);
    }

    public virtual Task<Result<PingResponse>> Ping(PingRequest request)
    {
        return this.RpcExecute<PingRequest, PingResponse>(request);
    }

    public virtual Task<Result<ProfileInfoResponse>> ProfileInfo(ProfileInfoRequest request)
    {
        return this.RpcExecute<ProfileInfoRequest, ProfileInfoResponse>(request);
    }

    public virtual Task<Result<RegisterResponse>> Register(RegisterRequest request)
    {
        return this.RpcExecute<RegisterRequest, RegisterResponse>(request);
    }
}