using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;

using NSubstitute;
using Xunit;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Server.Tests
{
    public class AspNetCoreHttpServerTest
    {
        private const string BindToAddress = "http://127.0.0.1:6297";
        
        [Fact]
        public async Task HttpServer_Responds_To_Request()
        {
            var config = new HttpServerConfig
            {
                BindAddresses = new [] { BindToAddress },
            };

            var log = Substitute.For<ILog>();

            await using (var server = new AspNetCoreHttpServerImpl(log, config))
            {
                await server.Start(async context =>
                {
                    string requestBody;
                    using (var streamReader = new StreamReader(context.Request.Body, EncodingHelper.UTF8))
                    {
                        requestBody = await streamReader.ReadToEndAsync();
                    }

                    int num = int.Parse(requestBody);

                    num += 1;

                    string responseBodyString = num.ToString();
                    byte[] responseBody = EncodingHelper.UTF8.GetBytes(responseBodyString);

                    context.Response.StatusCode = 200;
                    await context.Response.Body.WriteAsync(responseBody, 0, requestBody.Length);
                });

                var client = new HttpClient
                {
                    BaseAddress = new Uri(BindToAddress) 
                };

                int num = 42;

                var response = await client.PostAsync("/", new StringContent(num.ToString(), EncodingHelper.UTF8));
                byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
                string responseString = EncodingHelper.UTF8.GetString(responseBytes);

                int responseNum = int.Parse(responseString);
                
                Assert.Equal(200, (int)response.StatusCode);
                Assert.Equal(43, responseNum);
            }
        }
    }
}