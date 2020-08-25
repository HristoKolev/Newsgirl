namespace Newsgirl.Server.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Testing;
    using Xunit;

    public class RpcRequestHandlerTest
    {
        public class IncrementHandler
        {
            [RpcBind(typeof(IncrementTestRequest), typeof(IncrementTestResponse))]
            public Task<RpcResult<IncrementTestResponse>> Increment(IncrementTestRequest req)
            {
                var result = RpcResult.Ok(new IncrementTestResponse
                {
                    Num = req.Num + 1,
                });

                result.Headers.Add("test_key", "test_value");

                return Task.FromResult(result);
            }

            [RpcBind(typeof(ThrowingTestRequest), typeof(ThrowingTestResponse))]
            public Task<RpcResult<ThrowingTestResponse>> Increment(ThrowingTestRequest req)
            {
                throw new DetailedLogException("Throwing from inside of a handler.");
            }
        }

        public class ThrowingTestRequest { }

        public class ThrowingTestResponse { }

        public class IncrementTestResponse
        {
            public int Num { get; set; }
        }

        public class IncrementTestRequest
        {
            public int Num { get; set; }
        }

        public class TestHeadersMiddleware : RpcMiddleware
        {
            public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
            {
                await next(context, instanceProvider);

                var response = (Task<RpcResult<IncrementTestResponse>>) context.ResponseTask;

                foreach (var header in context.RequestMessage.Headers)
                {
                    response.Result.Headers.Add(header.Key, header.Value);
                }
            }
        }

        [Fact]
        public async Task Returns_error_on_empty_body()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.GetAsync("/");
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_invalid_json()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync($"/rpc/{nameof(IncrementTestRequest)}", new StringContent("not json"));
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_null_request_type()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/rpc/", new StringContent("{}"));
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_unknown_request_type()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/rpc/NotRegisteredRequest", new StringContent("{}"));
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_execution_error()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync($"/rpc/{nameof(ThrowingTestRequest)}", new StringContent("{ \"payload\": {} }"));
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_correct_result()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
                MiddlewareTypes = new[]
                {
                    typeof(TestHeadersMiddleware),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync(
                    $"/rpc/{nameof(IncrementTestRequest)}",
                    new StringContent("{ \"payload\": { \"num\": 33 }, \"headers\": { \"rpc-h1\": \"v1\" }  }")
                );

                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        private static RequestDelegate CreateHandler(RpcEngineOptions rpcEngineOptions)
        {
            var errorReporter = new ErrorReporterMock();
            var logMock = new StructuredLogMock();

            var rpcEngine = new RpcEngine(rpcEngineOptions);
            var instanceProvider = new FuncInstanceProvider(Activator.CreateInstance);
            var handler = new RpcRequestHandler(rpcEngine, instanceProvider, new AsyncLocalsImpl(), errorReporter, logMock);

            Task Handler(HttpContext context)
            {
                string requestType = RpcRequestHandler.ParseRequestType(context);

                return handler.HandleRequest(context, requestType);
            }

            return Handler;
        }

        private class FuncInstanceProvider : InstanceProvider
        {
            private readonly Func<Type, object> func;

            public FuncInstanceProvider(Func<Type, object> func)
            {
                this.func = func;
            }

            public object Get(Type type)
            {
                return this.func(type);
            }

            public T Get<T>()
            {
                return (T) this.Get(typeof(T));
            }
        }
    }
}
