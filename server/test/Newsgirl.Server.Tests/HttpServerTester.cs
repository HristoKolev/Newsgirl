namespace Newsgirl.Server.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using Shared.Infrastructure;

    /// <summary>
    ///     A testing service that facilitates testing of <see cref="RequestDelegate" />'s.
    ///     Creates <see cref="HttpServerImpl" /> with given <see cref="RequestDelegate" /> and starts it on a random port.
    ///     Creates an <see cref="HttpClient" /> with <see cref="HttpClient.BaseAddress" /> that points to the
    ///     <see cref="HttpServerImpl" />.
    ///     Disposes both <see cref="HttpServerImpl" /> and <see cref="HttpClient" /> when <see cref="DisposeAsync" /> is
    ///     called.
    /// </summary>
    public class HttpServerTester : IAsyncDisposable
    {
        private HttpServerTester()
        {
        }

        /// <summary>
        ///     This gets populated with an <see cref="Exception" /> object
        ///     if the given <see cref="RequestDelegate" /> throws.
        /// </summary>
        public Exception Exception { get; set; }

        public HttpServerImpl Server { get; set; }

        public HttpClient Client { get; set; }

        public async ValueTask DisposeAsync()
        {
            await this.Server.DisposeAsync();
            this.Client.Dispose();
        }

        /// <summary>
        ///     Creates and starts a new instance of <see cref="HttpServerTester" />
        /// </summary>
        public static async Task<HttpServerTester> Create(RequestDelegate requestDelegate)
        {
            var tester = new HttpServerTester();

            var serverConfig = new HttpServerConfig
            {
                Addresses = new[] {"http://127.0.0.1:0"}
            };

            var log = Substitute.For<ILog>();

            async Task Handler(HttpContext context)
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
            }

            var server = new HttpServerImpl(log, serverConfig, Handler);

            await server.Start();

            var client = new HttpClient
            {
                BaseAddress = new Uri(server.FirstAddress)
            };

            tester.Server = server;
            tester.Client = client;

            return tester;
        }
    }
}