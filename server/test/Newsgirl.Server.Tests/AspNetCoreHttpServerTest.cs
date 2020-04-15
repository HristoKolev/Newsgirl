using System;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Xunit;
using Newsgirl.Shared.Infrastructure;
using Newsgirl.Testing;

namespace Newsgirl.Server.Tests
{
    public class AspNetCoreHttpServerTest
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
                byte[] responseBody = EncodingHelper.UTF8.GetBytes(responseBodyString);

                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(responseBody, 0, requestBody.Length);
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                int num = 42;

                var response =
                    await tester.Client.PostAsync("/", new StringContent(num.ToString(), EncodingHelper.UTF8));
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

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.GetAsync("/");
                response.EnsureSuccessStatusCode();
                
                var responseBodyBytes = await response.Content.ReadAsByteArrayAsync();

                //  Check the sting for equality.
                string responseBodyString = EncodingHelper.UTF8.GetString(responseBodyBytes);
                Assert.Equal(resourceText, responseBodyString);

                // Check the bytes for equality.
                byte[] expectedBytes = EncodingHelper.UTF8.GetBytes(resourceText);
                AssertExt.EqualByteArray(expectedBytes, responseBodyBytes);
            }
        }
        
        [Theory]
        [InlineData("4000b_lipsum.txt")]
        [InlineData("unicode_test_page.html")]
        public async Task ReadUtf8_works_for_valid_utf8(string resourceName)
        {
            byte[] resourceBytes = await TestHelper.GetResourceBytes(resourceName);
            string resourceString = EncodingHelper.UTF8.GetString(resourceBytes);

            string str = null;
            
            async Task Handler(HttpContext context)
            {
                str = await context.Request.ReadUtf8();
                context.Response.StatusCode = 200;
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(resourceBytes));
                response.EnsureSuccessStatusCode();

                Assert.Equal(resourceString, str);
            }
        }
        
        [Fact]
        public async Task ReadUtf8_throws_on_invalid_utf8()
        {
            byte[] invalidUtf8 = await TestHelper.GetResourceBytes("app-vnd.flatpak-icon.png");

            Exception exception = null;
            
            async Task Handler(HttpContext context)
            {
                try
                {
                    await context.Request.ReadUtf8();
                }
                catch (Exception err)
                {
                    exception = err;
                } 
                
                context.Response.StatusCode = 200;
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(invalidUtf8));
                response.EnsureSuccessStatusCode();

                Snapshot.MatchError(exception);
            }
        }
    }
}