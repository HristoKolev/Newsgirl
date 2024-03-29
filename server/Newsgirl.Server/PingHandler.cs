namespace Newsgirl.Server;

using System.Threading.Tasks;
using Infrastructure;
using Xdxd.DotNet.Rpc;

[RpcAuth(RequiresAuthentication = false)]
public class PingHandler
{
    [RpcBind(typeof(PingRequest), typeof(PingResponse))]
    public Task<PingResponse> Ping(PingRequest req)
    {
        return Task.FromResult(new PingResponse
        {
            Message = "Works.",
        });
    }
}

public class PingResponse
{
    public string Message { get; set; }
}

public class PingRequest { }