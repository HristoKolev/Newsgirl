namespace Newsgirl.Server.Tests
{
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class PingHandlerTest : HttpServerAppTest
    {
        [Fact]
        public async Task Ping_returns_correct_result()
        {
            var result = await this.RpcClient.Ping(new PingRequest());

            Snapshot.Match(result);
        }
    }
}
