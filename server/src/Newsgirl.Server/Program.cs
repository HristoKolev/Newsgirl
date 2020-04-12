using System;
using System.Buffers;
using System.IO;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text.Json;
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

    public static class HttpServerHelpers
    {
        public static async ValueTask WriteUtf8(this HttpResponse response, string responseBodyString)
        {
            // The default
            var bufferPool = ArrayPool<byte>.Shared;

            // A big waste of memory. This ends up being 3x+ the length of the string, but we have to be safe.
            int maxByteCount = EncodingHelper.UTF8.GetMaxByteCount(responseBodyString.Length);
            
            // Borrowing from the default pool will allocate if writing a string that is more than ~350 000 characters.
            byte[] responseBuffer = bufferPool.Rent(maxByteCount);

            // Encode UTF8.
            int utf8EncodedByteLength;

            try
            {
                // EncoderNLS is or at least should be stateful.
                // I don't know how much it matters if I'm throwing on invalid input, but I cant risk to use it concurrently.
                // +1 allocation.
                var encoder = EncodingHelper.UTF8.GetEncoder();
                
                // The flush here should not matter,  
                utf8EncodedByteLength = encoder.GetBytes(responseBodyString, responseBuffer, true);
            }
            catch (Exception err)
            {
                bufferPool.Return(responseBuffer);

                throw new DetailedLogException("Failed to encode UTF8 response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY",
                };
            }

            // Write to response.
            try
            {
                var responseStream = response.Body;

                await responseStream.WriteAsync(responseBuffer, 0, utf8EncodedByteLength);
                await responseStream.FlushAsync();
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_RESPONSE_BODY",
                };
            }
            finally
            {
                bufferPool.Return(responseBuffer);
            }
        }
    }

    public class Model
    {
    }
}