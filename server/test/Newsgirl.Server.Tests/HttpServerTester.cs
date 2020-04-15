using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newsgirl.Shared.Infrastructure;
using NSubstitute;

namespace Newsgirl.Server.Tests
{
    public class HttpServerTester : IAsyncDisposable
    {
        public AspNetCoreHttpServerImpl Server { get; set; }

        public Exception Exception;

        public HttpClient Client { get; set; }

        protected HttpServerTester()
        {
        }

        public static async Task<HttpServerTester> Create(RequestDelegate requestDelegate)
        {
            var tester = new HttpServerTester();

            var serverConfig = new HttpServerConfig
            {
                BindAddresses = new[] {"http://127.0.0.1:0"},
            };

            var log = Substitute.For<ILog>();

            var server = new AspNetCoreHttpServerImpl(log, serverConfig);

            await server.Start(async context =>
            {
                try
                {
                    await requestDelegate(context);
                }
                catch (Exception err)
                {
                    tester.Exception = err;
                    throw;
                }
            });

            var serverAddresses = server.GetAddresses();

            var client = new HttpClient
            {
                BaseAddress = new Uri(serverAddresses.First())
            };

            tester.Server = server;
            tester.Client = client;

            return tester;
        }

        public ValueTask DisposeAsync()
        {
            return this.Server.DisposeAsync();
        }
    }
}