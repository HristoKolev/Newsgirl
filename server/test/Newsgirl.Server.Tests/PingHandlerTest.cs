namespace Newsgirl.Server.Tests
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Testing;
    using Xunit;

    public class PingHandlerTest : AppDatabaseTest
    {
        [Fact]
        public async Task Ping_returns_correct_result()
        {
            await using (var tester = await HttpServerAppTester.Create())
            {
                var rcpClient = new TestRpcClient(tester.App);

                var result = await rcpClient.Ping(new PingRequest());

                Snapshot.Match(result);
            }
        }
    }
}
