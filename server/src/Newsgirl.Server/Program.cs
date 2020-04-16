using System;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Server
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string address = "http://127.0.0.1:5000";

            var config = new HttpServerConfig
            {
                BindAddresses = new[]
                {
                    address
                },
            };

            var log = new CustomLogger(new CustomLoggerConfig
            {
                DisableSentryIntegration = true,
                DisableConsoleLogging = true,
            });

            var server = new AspNetCoreHttpServerImpl(log, config);

            await server.Start(ProcessRequest);

            var client = new HttpClient
            {
                BaseAddress = new Uri(address)
            };

            var random = new Random(123);

            string data = string.Join("", Enumerable.Range(0, 100 * 1024).Select(i => random.Next(i).ToString()));

            var response = await client.PostAsync("/", new StringContent(data, EncodingHelper.UTF8));

            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
            string responseString = EncodingHelper.UTF8.GetString(responseBytes);

            Console.WriteLine(data == responseString);

            await server.Stop();
        }

        private static async Task ProcessRequest(HttpContext context)
        {
            string responseBodyString = await context.Request.ReadUtf8();

            context.Response.StatusCode = 200;

            await context.Response.WriteUtf8(responseBodyString);
        }
    }
}