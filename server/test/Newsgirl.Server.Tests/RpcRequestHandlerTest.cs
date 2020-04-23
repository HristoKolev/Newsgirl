namespace Newsgirl.Server.Tests
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
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
        public async Task Works()
        {
            var rpcEngineOptions = new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler)
                },
            };

            RpcEngine rpcEngine = null;

            object ResolveType(Type t)
            {
                if (t == typeof(RpcEngine))
                {
                    return rpcEngine;
                }

                if (t == typeof(ILog))
                {
                    return Substitute.For<ILog>();
                }

                return Activator.CreateInstance(t);
            }

            var instanceProvider = new TestInstanceProvider(ResolveType);
            
            rpcEngine = new RpcEngine(rpcEngineOptions, Substitute.For<ILog>());

            Task Handler(HttpContext context)
            {
                return RpcRequestHandler.HandleRequest(context, instanceProvider);
            }

            string responseBody;
            
            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.GetAsync("/");
                response.EnsureSuccessStatusCode();
                
                responseBody = await response.Content.ReadAsStringAsync();
            }
            
            Snapshot.Match(responseBody);
        }
        
        public class TestInstanceProvider : InstanceProvider
        {
            private readonly Func<Type, object> func;

            public TestInstanceProvider(Func<Type, object> func)
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
