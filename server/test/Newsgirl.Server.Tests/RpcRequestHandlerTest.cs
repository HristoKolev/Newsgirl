namespace Newsgirl.Server.Tests
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using NSubstitute;
    using Shared;
    using Shared.Infrastructure;
    using Testing;
    using Xunit;

    public class RpcRequestHandlerTest
    {
        public class IncrementHandler
        {
            [RpcBind(typeof(IncrementTestRequest), typeof(IncrementTestResponse))]
            public Task<RpcResult<IncrementTestResponse>> Increment(IncrementTestRequest req)
            {
                return Task.FromResult(RpcResult.Ok(new IncrementTestResponse
                {
                    Num = req.Num + 1
                }));
            }
            
            [RpcBind(typeof(ThrowingTestRequest), typeof(ThrowingTestResponse))]
            public Task<RpcResult<ThrowingTestResponse>> Increment(ThrowingTestRequest req)
            {
                throw new DetailedLogException("Throwing from inside of a handler.");
            }
        }

        public class ThrowingTestRequest
        {
        }

        public class ThrowingTestResponse
        {
        }

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
                    typeof(IncrementHandler)
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
                    typeof(IncrementHandler)
                },
            });

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/", new StringContent("not json"));
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
                    typeof(IncrementHandler)
                },
            });

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/", new StringContent("{\"type\": null}"));
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
                    typeof(IncrementHandler)
                },
            });

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/", new StringContent("{\"type\": \"NotRegisteredRequest\"}"));
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
                    typeof(IncrementHandler)
                },
            });

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/", new StringContent("{\"type\": \"ThrowingTestRequest\", \"payload\":{} }"));
                response.EnsureSuccessStatusCode();
                
                responseBody = await response.Content.ReadAsStringAsync();
            }
            
            Snapshot.MatchJson(responseBody);
        }
        
        [Fact]
        public async Task Logs()
        {
            var handler = await CreateHandlerRealLog(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler)
                },
            });

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/", new StringContent("{\"type\": \"ThrowingTestRequest\", \"payload\":{} }"));
                response.EnsureSuccessStatusCode();
                
                responseBody = await response.Content.ReadAsStringAsync();
            }
            
            Snapshot.MatchJson(responseBody);
        }

        private static RequestDelegate CreateHandler(RpcEngineOptions rpcEngineOptions)
        {
            var log = CreateLogStub();
            var rpcEngine = new RpcEngine(rpcEngineOptions, log);
            var instanceProvider = new FuncInstanceProvider(Activator.CreateInstance);
            var asyncLocals = new AsyncLocalsImpl();
            var handler = new RpcRequestHandler(rpcEngine, log, instanceProvider, asyncLocals);
            Task Handler(HttpContext context) => handler.HandleRequest(context);
            return Handler;
        }
        
        private static async Task<RequestDelegate> CreateHandlerRealLog(RpcEngineOptions rpcEngineOptions)
        {
            var log = CreateLogStub();
            var rpcEngine = new RpcEngine(rpcEngineOptions, log);
            var instanceProvider = new FuncInstanceProvider(Activator.CreateInstance);
            var asyncLocals = new AsyncLocalsImpl();
            var handler = new RpcRequestHandler(rpcEngine, await CreateRealLog(asyncLocals), instanceProvider, asyncLocals);
            Task Handler(HttpContext context) => handler.HandleRequest(context);
            return Handler;
        }
        
        private static async Task<CustomLogger> CreateRealLog(AsyncLocals asyncLocals)
        {
            string appConfigPath = Path.GetFullPath("../../../newsgirl-server-test-config.json");
            var appConfig = JsonConvert.DeserializeObject<HttpServerAppConfig>(await File.ReadAllTextAsync(appConfigPath));
            var testLog = new CustomLogger(appConfig.Logging);
            testLog.AddSyncHook(asyncLocals.CollectHttpData);
            return testLog;
        }

        private static ILog CreateLogStub()
        {
            const string GUID = "61289445-04b7-4f59-bbdd-499c36861bc0";
            
            var log = Substitute.For<ILog>();
            log.Error(null).ReturnsForAnyArgs(x => GUID);
            log.Error(null, (string) null).ReturnsForAnyArgs(x => GUID);
            log.Error(null, (Dictionary<string, object>) null).ReturnsForAnyArgs(x => GUID);
            log.Error(null, null, null).ReturnsForAnyArgs(x => GUID);
            return log;
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
