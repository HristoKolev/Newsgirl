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
        public async Task Execute_throws_on_null_message_payload()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute<ExecutorTestResponse>(null);
            });
        }
        
        [Fact]
        public async Task Execute_throws_when_it_cannot_match_a_handler()
        {
            var executor = CreateExecutor();

            await Snapshot.MatchError(async () =>
            {
                await executor.Execute<NonRegisteredResponse>(new NonRegisteredRequest());
            });
        }
        
        [Fact]
        public async Task Execute_runs_the_handler_method()
        {
            var executor = CreateExecutor();

            ExecutorTestHandler.RunCount = 0;

            await executor.Execute<ExecutorTestResponse>(new ExecutorTestRequest());
            
            Assert.Equal(1, ExecutorTestHandler.RunCount);
        }
        
        [Fact]
        public async Task Execute_passes_the_request_to_the_handler_method()
        {
            var executor = CreateExecutor();

            var request = new ExecutorTestRequest();
            
            await executor.Execute<ExecutorTestResponse>(request);
            
            Assert.Equal(request, ExecutorTestHandler.Request);
        }
        
        [Fact]
        public async Task Execute_returns_the_response_returned_from_the_handler_method()
        {
            var executor = CreateExecutor();

            var request = new ExecutorTestRequest
            {
                Number = 42
            };
            
            var result = await executor.Execute<ExecutorTestResponse>(request);
            
            Assert.Equal(result.Payload, ExecutorTestHandler.Response);
            
            Assert.Equal(43, result.Payload.Number);
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
    
    public class NonRegisteredResponse { }
}
