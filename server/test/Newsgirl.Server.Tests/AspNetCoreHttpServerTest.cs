using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using NSubstitute;
using Xunit;
using Newsgirl.Shared.Infrastructure;
using Newsgirl.Testing;

namespace Newsgirl.Server.Tests
{
    public class AspNetCoreHttpServerTest
    {
        private const string BindToAddress = "http://127.0.0.1:6297";

        [Fact]
        public async Task HttpServer_Responds_To_Requests()
        {
            var config = new HttpServerConfig
            {
                BindAddresses = new[] {BindToAddress},
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

                Assert.Equal(200, (int) response.StatusCode);
                Assert.Equal(43, responseNum);
            }
        }
    }

    public class HttpServerHelperTest
    {
        [Theory]
        [InlineData("4000b_lipsum.txt", 8192)]
        [InlineData("4000b_lipsum.txt", 100)]
        [InlineData("4000b_lipsum.txt", 1)]
        [InlineData("unicode_test_page.html", 8192)]
        [InlineData("unicode_test_page.html", 100)]
        [InlineData("unicode_test_page.html", 1)]
        public async Task WriteUtf8_works(string resourceName, int batchSize)
        {
            string resourceText = await TestHelper.GetResourceText(resourceName);

            async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteUtf8(resourceText, batchSize);
            }

            await using (var tester = await AspNetCoreHttpServerTester.Create(Handler))
            {
                var response = await tester.Client.GetAsync("/");
                var responseBodyBytes = await response.Content.ReadAsByteArrayAsync();

                //  Check the sting for equality.
                string responseBodyString = EncodingHelper.UTF8.GetString(responseBodyBytes);
                Assert.Equal(resourceText, responseBodyString);

                // Check the bytes for equality.
                byte[] expectedBytes = EncodingHelper.UTF8.GetBytes(resourceText);
                AssertExt.EqualByteArray(expectedBytes, responseBodyBytes);
            }
        }
    }

    public class AspNetCoreHttpServerTester : IAsyncDisposable
    {
        private const string BindToAddress = "http://127.0.0.1:6297";

        public AspNetCoreHttpServerImpl Server { get; set; }

        public Exception Exception;

        public HttpClient Client { get; set; }

        protected AspNetCoreHttpServerTester()
        {
        }

        public static async Task<AspNetCoreHttpServerTester> Create(RequestDelegate requestDelegate)
        {
            var tester = new AspNetCoreHttpServerTester();

            var serverConfig = new HttpServerConfig
            {
                BindAddresses = new[] {BindToAddress},
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

            var client = new HttpClient
            {
                BaseAddress = new Uri(BindToAddress)
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