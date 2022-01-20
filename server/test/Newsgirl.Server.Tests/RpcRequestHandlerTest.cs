namespace Newsgirl.Server.Tests;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Infrastructure;
using Microsoft.AspNetCore.Http;
using Xdxd.DotNet.Http;
using Xdxd.DotNet.Rpc;
using Xdxd.DotNet.Shared;
using Xdxd.DotNet.Testing;
using Xunit;

public class RpcRequestHandlerTest
{
    public class IncrementHandler
    {
        [RpcBind(typeof(IncrementTestRequest), typeof(IncrementTestResponse))]
        public Task<Result<IncrementTestResponse>> Increment(IncrementTestRequest req)
        {
            var result = Result.Ok(new IncrementTestResponse
            {
                Num = req.Num + 1,
            });

            return Task.FromResult(result);
        }

        [RpcBind(typeof(ThrowingTestRequest), typeof(ThrowingTestResponse))]
        public Task<Result<ThrowingTestResponse>> Increment(ThrowingTestRequest req)
        {
            throw new DetailedException("Throwing from inside of a handler.");
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
            var response = await tester.Client.GetAsync($"/rpc/{nameof(IncrementTestRequest)}");
            tester.EnsureHandlerSuccess();
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
            tester.EnsureHandlerSuccess();
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
            tester.EnsureHandlerSuccess();
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
            tester.EnsureHandlerSuccess();
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
            var response = await tester.Client.PostAsync($"/rpc/{nameof(ThrowingTestRequest)}", new StringContent("{}"));
            tester.EnsureHandlerSuccess();
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
        });

        string responseBody;

        await using (var tester = await HttpServerTester.Create(handler))
        {
            var httpRequestMessage = new HttpRequestMessage
            {
                RequestUri = new Uri($"/rpc/{nameof(IncrementTestRequest)}", UriKind.Relative),
                Content = new StringContent("{ \"num\": 33 }"),
            };

            var response = await tester.Client.SendAsync(httpRequestMessage);

            tester.EnsureHandlerSuccess();

            response.EnsureSuccessStatusCode();

            responseBody = await response.Content.ReadAsStringAsync();
        }

        Snapshot.MatchJson(responseBody);
    }

    private static RequestDelegate CreateHandler(RpcEngineOptions rpcEngineOptions)
    {
        var errorReporter = new ErrorReporterMock();

        var rpcEngine = new RpcEngine(rpcEngineOptions);

        var instanceProvider = new FuncInstanceProvider(Activator.CreateInstance);

        Task Handler(HttpContext context)
        {
            var httpRequestState = new HttpRequestState
            {
                HttpContext = context,
                RpcState = new RpcRequestState(),
            };

            var handler = new RpcRequestHandler(
                rpcEngine,
                instanceProvider,
                errorReporter
            );

            var requestPath = context.Request.Path;
            httpRequestState.RpcState.RpcRequestType = requestPath.Value!.Remove(0, "/rpc/".Length);
            return handler.HandleRpcRequest(httpRequestState);
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
            return (T)this.Get(typeof(T));
        }
    }
}
