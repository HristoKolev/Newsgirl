namespace Newsgirl.Server.Tests
{
    using System.IO;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using NSubstitute;
    using Shared.Infrastructure;
    using Xunit;

    public class HttpServerTest
    {
        [Fact]
        public async Task HttpServer_Responds_To_Requests()
        {
            static async Task Handler(HttpContext context)
            {
                string requestBody;
                using (var streamReader = new StreamReader(context.Request.Body, EncodingHelper.UTF8))
                {
                    requestBody = await streamReader.ReadToEndAsync();
                }

                int num = int.Parse(requestBody);

                num += 1;

                string responseBodyString = num.ToString();
                var responseBody = EncodingHelper.UTF8.GetBytes(responseBodyString);

                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(responseBody, 0, requestBody.Length);
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                int num = 42;

                var response =
                    await tester.Client.PostAsync("/", new StringContent(num.ToString(), EncodingHelper.UTF8));
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                string responseString = EncodingHelper.UTF8.GetString(responseBytes);

                int responseNum = int.Parse(responseString);

                Assert.Equal(200, (int) response.StatusCode);
                Assert.Equal(43, responseNum);
            }
        }
        
        [Fact]
        public async Task HttpServer_shuts_down_correctly()
        {
            var serverConfig = new HttpServerConfig
            {
                Addresses = new[] {"http://127.0.0.1:56962"}
            };

            var log = Substitute.For<ILog>();
            static Task Handler(HttpContext context) => Task.CompletedTask;
            var server = new HttpServerImpl(log, serverConfig, Handler);

            for (int i = 0; i < 10; i++)
            {
                await server.Start();
                await server.Stop();    
            }
        }
    }
}
