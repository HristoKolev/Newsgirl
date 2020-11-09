namespace Newsgirl.Server.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Http;
    using NSubstitute;
    using Shared;
    using Xunit;

    public class RpcAuthorizationMiddlewareTest
    {
        [Fact]
        public async Task disallows_access_anon_session_default_handler()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PrivateByDefaultRequest(),
                Type = nameof(TestHandler.PrivateByDefaultRequest),
            };

            var authResult = AuthResult.Anonymous;

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            AssertUnauthorizedAccess(result);
        }

        [Fact]
        public async Task disallows_access_anon_session_explicitly_private_handler()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PrivateRequest(),
                Type = nameof(TestHandler.PrivateRequest),
            };

            var authResult = AuthResult.Anonymous;

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            AssertUnauthorizedAccess(result);
        }

        [Fact]
        public async Task allows_access_valid_session_private_handler()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PrivateRequest(),
                Type = nameof(TestHandler.PrivateRequest),
            };

            var authResult = new AuthResult
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                ValidCsrfToken = true,
            };

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.True(result.IsOk);
        }

        [Fact]
        public async Task disallows_access_valid_user_invalid_csrf_token()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PrivateRequest(),
                Type = nameof(TestHandler.PrivateRequest),
            };

            var authResult = new AuthResult
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                ValidCsrfToken = false,
            };

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            AssertUnauthorizedAccess(result);
        }

        [Fact]
        public async Task allows_access_anon_session_public_handler()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PublicRequest(),
                Type = nameof(TestHandler.PublicRequest),
            };

            var authResult = AuthResult.Anonymous;

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.True(result.IsOk);
        }

        [Fact]
        public async Task allows_access_valid_session_public_handler()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PublicRequest(),
                Type = nameof(TestHandler.PublicRequest),
            };

            var authResult = new AuthResult
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                ValidCsrfToken = true,
            };

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.True(result.IsOk);
        }

        [Fact]
        public async Task injects_auth_result_handler_parameter()
        {
            var rpcEngine = CreateRpcEngine();

            var requestMessage = new RpcRequestMessage
            {
                Payload = new TestHandler.PrivateWithAuthParameterRequest(),
                Type = nameof(TestHandler.PrivateWithAuthParameterRequest),
            };

            var authResult = new AuthResult
            {
                SessionID = 1,
                LoginID = 1,
                ProfileID = 1,
                ValidCsrfToken = true,
            };

            var instanceProvider = GetInstanceProvider(new HttpRequestState {AuthResult = authResult});

            var result = await rpcEngine.Execute(requestMessage, instanceProvider);

            Assert.True(result.IsOk);

            var response = (TestHandler.PrivateWithAuthParameterResponse) result.Payload;

            Assert.Equal(authResult, response.AuthResult);
        }

        private static RpcEngine CreateRpcEngine()
        {
            return new RpcEngine(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[] {typeof(TestHandler)},
                MiddlewareTypes = new[] {typeof(RpcAuthorizationMiddleware)},
                ParameterTypeWhitelist = new[] {typeof(AuthResult)},
            });
        }

        public class TestHandler
        {
            [RpcBind(typeof(PrivateByDefaultRequest), typeof(PrivateByDefaultResponse))]
            public Task<PrivateByDefaultResponse> PrivateByDefault(PrivateByDefaultRequest req)
            {
                return Task.FromResult(new PrivateByDefaultResponse());
            }

            [RpcAuth(RequiresAuthentication = true)]
            [RpcBind(typeof(PrivateRequest), typeof(PrivateResponse))]
            public Task<PrivateResponse> Private(PrivateRequest req)
            {
                return Task.FromResult(new PrivateResponse());
            }

            [RpcAuth(RequiresAuthentication = false)]
            [RpcBind(typeof(PublicRequest), typeof(PublicResponse))]
            public Task<PublicResponse> Public(PublicRequest req)
            {
                return Task.FromResult(new PublicResponse());
            }

            [RpcBind(typeof(PrivateWithAuthParameterRequest), typeof(PrivateWithAuthParameterResponse))]
            public Task<PrivateWithAuthParameterResponse> PrivateWithAuthParameter(PrivateWithAuthParameterRequest req, AuthResult authResult)
            {
                return Task.FromResult(new PrivateWithAuthParameterResponse
                {
                    AuthResult = authResult,
                });
            }

            public class PrivateWithAuthParameterRequest { }

            public class PrivateWithAuthParameterResponse
            {
                public AuthResult AuthResult { get; set; }
            }

            public class PublicRequest { }

            public class PublicResponse { }

            public class PrivateRequest { }

            public class PrivateResponse { }

            public class PrivateByDefaultRequest { }

            public class PrivateByDefaultResponse { }
        }

        private static void AssertUnauthorizedAccess(Result result)
        {
            Assert.False(result.IsOk);
            Assert.Equal(RpcAuthorizationMiddleware.UNAUTHORIZED_ACCESS_MESSAGE, result.ErrorMessages.Single());
        }

        private static InstanceProvider GetInstanceProvider(HttpRequestState httpRequestState)
        {
            var resolver = Substitute.For<InstanceProvider>();
            resolver.Get(null).ReturnsForAnyArgs(x =>
            {
                var type = x.Arg<Type>();

                if (type == typeof(RpcAuthorizationMiddleware))
                {
                    return new RpcAuthorizationMiddleware(httpRequestState);
                }

                return Activator.CreateInstance(type);
            });
            return resolver;
        }
    }
}
