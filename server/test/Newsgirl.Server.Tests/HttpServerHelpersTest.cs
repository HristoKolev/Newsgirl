namespace Newsgirl.Server.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Sockets;
    using System.Text;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Shared;
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
                AssertExt.SequentialEqual(expectedBytes, responseBodyBytes);
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

        [Theory]
        [InlineData("4000b_lipsum.txt")]
        [InlineData("unicode_test_page.html")]
        public async Task ReadToEnd_works(string resourceName)
        {
            var resourceBytes = await TestHelper.GetResourceBytes(resourceName);

            async Task Handler(HttpContext context)
            {
                using var dataHandle = await context.Request.ReadToEnd();
                context.Response.StatusCode = 200;
                await context.Response.Body.WriteAsync(dataHandle.Memory);
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var response = await tester.Client.PostAsync("/", new ReadOnlyMemoryContent(resourceBytes));
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsByteArrayAsync();

                AssertExt.SequentialEqual(resourceBytes, responseBody);
            }
        }

        [Fact]
        public async Task ReadToEnd_throws_on_network_error()
        {
            static async Task Handler(HttpContext context)
            {
                using var dataHandle = await context.Request.ReadToEnd();
                context.Response.StatusCode = 200;
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                for (int i = 0; i < 100; i++)
                {
                    var uri = new Uri(tester.Server.BoundAddresses.First());

                    var socket = new Socket(SocketType.Stream, ProtocolType.Tcp)
                    {
                        LingerState = new LingerOption(true, 0), NoDelay = true,
                    };

                    socket.Connect(new IPEndPoint(IPAddress.Parse(uri.Host), uri.Port));

                    string requestJson = "{\"type\": \"PingRequest\", \"payload\":{ \"data\": \"dd\" } }";
                    var requestBody = JsonSerializer.SerializeToUtf8Bytes(requestJson);

                    var requestHeaderBuilder = new StringBuilder();
                    requestHeaderBuilder.Append("POST / HTTP/1.1\r\n");
                    requestHeaderBuilder.Append($"Host: {uri.Host}\r\n");
                    requestHeaderBuilder.Append("Content-Length: " + requestBody.Length + "\r\n");
                    requestHeaderBuilder.Append("Connection: close\r\n");
                    requestHeaderBuilder.Append("\r\n");

                    socket.Send(Encoding.ASCII.GetBytes(requestHeaderBuilder.ToString()), SocketFlags.None);
                    socket.Send(requestBody, 0, requestBody.Length / 2, SocketFlags.None);
                    socket.Close();

                    socket = null;

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                }

                Snapshot.MatchError(tester.Exception);
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
                _ = await tester.Client.GetAsync("/");

                Snapshot.MatchError(exception);
            }
        }

        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(100)]
        [InlineData(1023)]
        [InlineData(1024)]
        [InlineData(1025)]
        [InlineData(2047)]
        [InlineData(2048)]
        [InlineData(2049)]
        [InlineData(1024 * 100)]
        public async Task ReadUnknownSizeStream_works_with_different_sizes(int numberOfBytes)
        {
            static byte[] GetRandomBytes(int length)
            {
                var random = new Random(123);
                var array = new byte[length];

                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = (byte) random.Next(0, byte.MaxValue + 1);
                }

                return array;
            }

            var data = GetRandomBytes(numberOfBytes);

            Stream memoryStream = new MemoryStream(data);

            var read = await memoryStream.ReadUnknownSizeStream();

            var actualSequence = read.Memory.ToArray();

            AssertExt.SequentialEqual(data, actualSequence);
        }
    }
}
