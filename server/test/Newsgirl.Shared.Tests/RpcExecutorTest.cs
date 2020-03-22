using System;
using System.Threading.Tasks;

using Newsgirl.Testing;
using NSubstitute;
using Xunit;

namespace Newsgirl.Shared.Tests
{
    public class RpcExecutorTest
    {
        [Fact]
        public async Task Execute_throws_on_null_message_name()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute(null, null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_empty_message_name()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute(string.Empty, null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_whitespace_message_name()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute("   \t \r\n   ", null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_on_null_message_payload()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute("TestRequest", null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_when_it_cannot_match_a_handler()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute(nameof(NonRegisteredRequest), new NonRegisteredRequest());
            });
        }
        
        [Fact]
        public async Task Execute_runs_the_handler_method()
        {
            var executor = CreateExecutor();

            await executor.Execute(nameof(ExecutorTestRequest), new ExecutorTestRequest());
            
            Assert.Equal(1, ExecutorTestHandler.RunCount);
        }

        private static RpcExecutor CreateExecutor()
        {
            var metadata = new RpcMetadataScanner().ScanTypes(new[]
            {
                typeof(ExecutorTestHandler)
            });
            
            var resolver = Substitute.For<IoCResolver>();
            resolver.Resolve(null).ReturnsForAnyArgs(x => Activator.CreateInstance(x.Arg<Type>()));
            
            return new RpcExecutor(metadata, resolver);
        }

        public class ExecutorTestHandler
        {
            public static int RunCount = 0;

            public static ExecutorTestRequest Request;
            
            public static ExecutorTestResponse Response;
            
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public async Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                Request = req;
                RunCount += 1;

                var response = new ExecutorTestResponse
                {
                    Number = req.Number + 1,
                };

                Response = response;

                return response;
            }
        }
    }

    public class ExecutorTestRequest
    {
        public int Number { get; set; }
    }
    
    public class ExecutorTestResponse
    {
        public int Number { get; set; }
    }


    public class NonRegisteredRequest { }
}
