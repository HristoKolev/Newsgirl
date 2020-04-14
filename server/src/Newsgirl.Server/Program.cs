using System;
using System.Buffers;
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
            // Read request bytes.
            int contentLength = (int) context.Request.ContentLength.Value;
            var bufferPool = ArrayPool<byte>.Shared;
            var requestStream = context.Request.Body;

            byte[] requestBuffer = bufferPool.Rent(contentLength);

            try
            {
                while (await requestStream.ReadAsync(requestBuffer) > 0)
                {
                }
            }
            catch (Exception err)
            {
                bufferPool.Return(requestBuffer);

                throw new DetailedLogException("An error occurred while reading the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                    Details =
                    {
                        {"contentLength", contentLength},
                    }
                };
            }

            // Decode UTF8.
            static string DecodeUtf8(byte[] bytes, int length)
            {
                return EncodingHelper.UTF8.GetString(new ReadOnlySpan<byte>(bytes, 0, length));
            }

            string requestBodyString;

            try
            {
                requestBodyString = DecodeUtf8(requestBuffer, contentLength);
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to decode UTF8 from HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_DECODE_UTF8_REQUEST_BODY",
                    Details =
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestBuffer, 0, contentLength)}
                    }
                };
            }
            finally
            {
                bufferPool.Return(requestBuffer);
            }

            // Process request data.
            string responseBodyString = requestBodyString;

            context.Response.StatusCode = 200;

            await context.Response.WriteUtf8(responseBodyString);
        }
    }
}