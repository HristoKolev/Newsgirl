// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnusedMember.Global
// ReSharper disable UnusedParameter.Global

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
    using Autofac;
    using Autofac.Core;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Testing;
    using Xunit;

    public class HttpServerRespondsToRequests
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

                var response = await tester.Client.PostAsync("/", new StringContent(num.ToString(), EncodingHelper.UTF8));
                var responseBytes = await response.Content.ReadAsByteArrayAsync();
                string responseString = EncodingHelper.UTF8.GetString(responseBytes);

                int responseNum = int.Parse(responseString);

                Assert.Equal(200, (int) response.StatusCode);
                Assert.Equal(43, responseNum);
            }
        }
    }

    public class HttpServerShutsDownCorrectly
    {
        [Fact]
        public async Task HttpServer_shuts_down_correctly()
        {
            static Task Handler(HttpContext context)
            {
                return Task.CompletedTask;
            }

            var server = new CustomHttpServerImpl();

            for (int i = 0; i < 10; i++)
            {
                await server.Start(Handler, new[] {"http://127.0.0.1:0"});
                await server.Stop();
            }
        }
    }

    public class WriteUtf8Works
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

                tester.EnsureHandlerSuccess();
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
    }

    public class ReadUtf8WorksForValidUtf8
    {
        [Theory]
        [InlineData("4000b_lipsum.txt")]
        [InlineData("unicode_test_page.html")]
        public async Task ReadUtf8_works_for_valid_utf8(string resourceName)
        {
            for (int i = 0; i < 1000; i++)
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
                    tester.EnsureHandlerSuccess();
                    response.EnsureSuccessStatusCode();

                    Assert.Equal(resourceString, str);
                }
            }
        }
    }

    public class ReadToEndWorks
    {
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
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                var responseBody = await response.Content.ReadAsByteArrayAsync();

                AssertExt.SequentialEqual(resourceBytes, responseBody);
            }
        }
    }

    public class ReadToEndThrowsOnNetworkError
    {
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
                for (int i = 0; i < 200; i++)
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

                    // ReSharper disable once RedundantAssignment
                    socket = null;

                    GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                }

                Snapshot.MatchError(tester.Exception);
            }
        }
    }

    public class ReadUtf8ThrowsOnInvalidUtf8
    {
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
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                Snapshot.MatchError(exception);
            }
        }
    }

    public class WriteUtf8ThrowsOnInvalidInput
    {
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
    }

    public class ReadUnknownSizeStreamWorksWithDifferentSizes
    {
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

    public class HttpServerAppTestServerShutsDownCorrectly : HttpServerAppTest
    {
        [Fact]
        public async Task Server_Shuts_Down_Correctly()
        {
            var shutdownTask = Task.Run(async () =>
            {
                await Task.Delay(100);

                this.App.TriggerShutdown();
            });

            await this.App.AwaitShutdownTrigger();

            await shutdownTask;
        }
    }

    public class HttpServerAppTestIoCResolvesAllRegisteredTypes : HttpServerAppTest
    {
        [Fact]
        public void IoC_Resolves_All_Registered_Types()
        {
            var ignored = new[]
            {
                typeof(ILifetimeScope),
                typeof(IComponentContext),
            };

            var registeredTypes = this.App.IoC.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .Select(x => ((TypedService) x).ServiceType)
                .Where(x => !ignored.Contains(x))
                .ToList();

            foreach (var registeredType in registeredTypes)
            {
                this.App.IoC.Resolve(registeredType);
            }
        }
    }

    public class HttpServerAppTestRespondsToRequest : HttpServerAppTest
    {
        [Fact]
        public async Task Responds_to_request()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(this.App.GetAddress()),
            };

            var response = await client.PostAsync($"/rpc/{nameof(PingRequest)}", new StringContent("{ \"payload\": {} }"));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Snapshot.MatchJson(responseBody);
        }
    }

    public class RpcRequestHandlerTest
    {
        public class IncrementHandler
        {
            [RpcBind(typeof(IncrementTestRequest), typeof(IncrementTestResponse))]
            public Task<RpcResult<IncrementTestResponse>> Increment(IncrementTestRequest req)
            {
                var result = RpcResult.Ok(new IncrementTestResponse
                {
                    Num = req.Num + 1,
                });

                return Task.FromResult(result);
            }

            [RpcBind(typeof(ThrowingTestRequest), typeof(ThrowingTestResponse))]
            public Task<RpcResult<ThrowingTestResponse>> Increment(ThrowingTestRequest req)
            {
                throw new DetailedLogException("Throwing from inside of a handler.");
            }
        }

        public class ThrowingTestRequest { }

        public class ThrowingTestResponse { }

        public class IncrementTestResponse
        {
            public int Num { get; set; }
        }

        public class IncrementTestRequest
        {
            public int Num { get; set; }
        }

        [Fact]
        public async Task Returns_error_on_empty_body()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.GetAsync($"/rpc/{nameof(IncrementTestRequest)}");
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_invalid_json()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync($"/rpc/{nameof(IncrementTestRequest)}", new StringContent("not json"));
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_null_request_type()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/rpc/", new StringContent("{}"));
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_unknown_request_type()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync("/rpc/NotRegisteredRequest", new StringContent("{}"));
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_error_on_execution_error()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var response = await tester.Client.PostAsync($"/rpc/{nameof(ThrowingTestRequest)}", new StringContent("{}"));
                tester.EnsureHandlerSuccess();
                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        [Fact]
        public async Task Returns_correct_result()
        {
            var handler = CreateHandler(new RpcEngineOptions
            {
                PotentialHandlerTypes = new[]
                {
                    typeof(IncrementHandler),
                },
            });

            string responseBody;

            await using (var tester = await HttpServerTester.Create(handler))
            {
                var httpRequestMessage = new HttpRequestMessage
                {
                    RequestUri = new Uri($"/rpc/{nameof(IncrementTestRequest)}", UriKind.Relative),
                    Content = new StringContent("{ \"num\": 33 }"),
                };

                var response = await tester.Client.SendAsync(httpRequestMessage);

                tester.EnsureHandlerSuccess();

                response.EnsureSuccessStatusCode();

                responseBody = await response.Content.ReadAsStringAsync();
            }

            Snapshot.MatchJson(responseBody);
        }

        private static RequestDelegate CreateHandler(RpcEngineOptions rpcEngineOptions)
        {
            var errorReporter = new ErrorReporterMock();
            var logMock = new StructuredLogMock();

            var rpcEngine = new RpcEngine(rpcEngineOptions);

            var instanceProvider = new FuncInstanceProvider(Activator.CreateInstance);

            var handler = new RpcRequestHandler(
                rpcEngine,
                instanceProvider,
                new AsyncLocalsImpl(),
                errorReporter,
                TestHelper.DateTimeServiceStub,
                logMock
            );

            Task Handler(HttpContext context)
            {
                var requestPath = context.Request.Path;
                string rpcRequestType = requestPath.Value.Remove(0, "/rpc/".Length);
                return handler.HandleRequest(context, rpcRequestType);
            }

            return Handler;
        }

        private class FuncInstanceProvider : InstanceProvider
        {
            private readonly Func<Type, object> func;

            public FuncInstanceProvider(Func<Type, object> func)
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
