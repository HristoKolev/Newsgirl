namespace Newsgirl.Server.Tests
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared.Infrastructure;
    using Testing;
    using Xunit;

    public class HttpServerHelpersTest
    {
        [Theory]
        [InlineData("4000b_lipsum.txt")]
        [InlineData("unicode_test_page.html")]
        public async Task WriteUtf8_works(string resourceName)
        {
            string resourceText = await TestHelper.GetResourceText(resourceName);

            async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteUtf8(resourceText);
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
                var expectedBytes = EncodingHelper.UTF8.GetBytes(resourceText);
                AssertExt.EqualByteArray(expectedBytes, responseBodyBytes);
            }
        }

        [Theory]
        [InlineData("4000b_lipsum.txt")]
        [InlineData("unicode_test_page.html")]
        public async Task ReadUtf8_works_for_valid_utf8(string resourceName)
        {
            var resourceBytes = await TestHelper.GetResourceBytes(resourceName);
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
            var invalidUtf8 = await TestHelper.GetResourceBytes("app-vnd.flatpak-icon.png");

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

        [Fact]
        public async Task WriteUtf8_throws_on_invalid_input()
        {
            Exception exception = null;

            async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;

                try
                {
                    await context.Response.WriteUtf8("X\uD800Y");
                }
                catch (Exception err)
                {
                    exception = err;
                }
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.GetAsync("/");

                Snapshot.MatchError(exception);
            }
        }
    }
}