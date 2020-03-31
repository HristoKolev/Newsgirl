using System;
using System.Runtime.ExceptionServices;
using System.Threading.Tasks;

using Newsgirl.Testing;
using NSubstitute;
using Sentry.PlatformAbstractions;
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
            
            var response = await executor.Execute<ExecutorTestResponse>(request);
            
            Assert.Equal(response, ExecutorTestHandler.Response);
            
            Assert.Equal(43, response.Number);
        }
        
        [Fact]
        public async Task Execute_throws_when_the_handler_method_throws()
        {
            var rpcMetadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ThrowingExecutorTestHandler)
                },
            });

            var resolver = Substitute.For<InstanceProvider>();
            resolver.Get(null).ReturnsForAnyArgs(x => Activator.CreateInstance(x.Arg<Type>()));
            
            var executor = new RpcExecutor(rpcMetadataCollection, resolver);

            bool handled = false;

            try
            {
                await executor.Execute<ExecutorTestResponse>(new ExecutorTestRequest());

            }
            catch (Exception exception)
            {
                Assert.Equal(exception, ThrowingExecutorTestHandler.Exception);

                handled = true;
            }

            if (!handled)
            {
                throw new ApplicationException("The executor did not throw when the handler method did.");
            }
        }
        
        
        public class ThrowingExecutorTestHandler
        {
            public static Exception Exception;
            
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                var ex = new ApplicationException("Testing the exception handling.");

                Exception = ex;

                throw ex;
            }
        }

        private static RpcExecutor CreateExecutor()
        {
            var metadataCollection = RpcMetadataCollection.Build(new RpcMetadataBuildParams
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(ExecutorTestHandler)
                },
            });
            
            var resolver = Substitute.For<InstanceProvider>();
            
            resolver.Get(null).ReturnsForAnyArgs(x => Activator.CreateInstance(x.Arg<Type>()));
            
            return new RpcExecutor(metadataCollection, resolver);
        }

        public class ExecutorTestHandler
        {
            public static int RunCount = 0;

            public static ExecutorTestRequest Request;
            
            public static ExecutorTestResponse Response;
            
            [RpcBind(typeof(ExecutorTestRequest), typeof(ExecutorTestResponse))]
            public Task<ExecutorTestResponse> RpcMethod1(ExecutorTestRequest req)
            {
                Request = req;
                RunCount += 1;

                var response = new ExecutorTestResponse
                {
                    Number = req.Number + 1,
                };

                Response = response;

                return Task.FromResult(response);
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
