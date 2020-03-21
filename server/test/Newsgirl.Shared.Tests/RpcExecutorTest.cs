using System.Threading.Tasks;
using Xunit;

using Newsgirl.Testing;

namespace Newsgirl.Shared.Tests
{
    public class RpcExecutorTest
    {
        [Fact]
        public async Task Execute_throws_on_null_message_name()
        {
            var executor = new RpcExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute(null, null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_empty_message_name()
        {
            var executor = new RpcExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute(string.Empty, null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_whitespace_message_name()
        {
            var executor = new RpcExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute("   \t \r\n   ", null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_null_message_payload()
        {
            var executor = new RpcExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute("TestRequest", null);
            });
        }
    }
}
