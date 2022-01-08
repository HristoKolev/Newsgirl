namespace Newsgirl.Server.Tests
{
    using System;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;
    using Http;
    using NSubstitute;
    using Shared;
    using Xunit;

    public class RpcInputValidationMiddlewareTest
    {
        [Fact]
        public async Task returns_error_on_invalid_request()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.TestRequest(),
                Type = nameof(TestHandler.TestRequest),
            };

            var instanceProvider = GetInstanceProvider();

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.False(result.IsOk);
            Assert.Equal("__Number__", result.ErrorMessages.Single());
        }

        [Fact]
        public async Task returns_success_on_valid_request()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.TestRequest
                {
                    Number = 1,
                },
                Type = nameof(TestHandler.TestRequest),
            };

            var instanceProvider = GetInstanceProvider();

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.True(result.IsOk);

            var response = (TestHandler.TestResponse)result.Payload;

            Assert.Equal(2, response.Number);
        }

        public class TestHandler
        {
            [RpcBind(typeof(TestRequest), typeof(TestResponse))]
            public Task<TestResponse> Test(TestRequest req)
            {
                return Task.FromResult(new TestResponse
                {
                    // ReSharper disable once PossibleInvalidOperationException
                    Number = req.Number.Value + 1,
                });
            }

            public class TestRequest
            {
                [Required(ErrorMessage = "__Number__")]
                public int? Number { get; set; }
            }

            public class TestResponse
            {
                public int Number { get; set; }
            }
        }

        private static RpcEngine CreateRpcEngine()
        {
            return new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[] { typeof(TestHandler) },
                MiddlewareTypes = new[] { typeof(RpcInputValidationMiddleware) },
            });
        }

        private static InstanceProvider GetInstanceProvider()
        {
            var resolver = Substitute.For<InstanceProvider>();
            resolver.Get(null).ReturnsForAnyArgs(x => Activator.CreateInstance(x.Arg<Type>()));
            return resolver;
        }
    }
}
