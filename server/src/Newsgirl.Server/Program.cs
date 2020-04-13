using System;
using System.Buffers;
using System.IO.Pipelines;
using System.Linq;
using System.Net.Http;
using System.Text;
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
        private static void WriteBatch(ReadOnlySpan<char> batchCharacters, PipeWriter bodyWriter, int batchBufferSize, Encoder encoder, bool flush)
        {
            try
            {
                var buffer = bodyWriter.GetSpan(batchBufferSize);
                
                int bytesWritten = encoder.GetBytes(batchCharacters, buffer, flush);

                bodyWriter.Advance(bytesWritten);    
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to encode UTF8 response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY",
                };
            }
        }

        private static async ValueTask FlushPipe(PipeWriter bodyWriter)
        {
            try
            {
                await bodyWriter.FlushAsync();
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write to HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_TO_RESPONSE_BODY",
                };
            }
        }
        
        public static async ValueTask WriteUtf8(this HttpResponse response, string responseBodyString)
        {
            var pipeWriter = response.BodyWriter;
            
            // EncoderNLS is or at least should be stateful.
            // +1 allocation.
            var encoder = EncodingHelper.UTF8.GetEncoder();
            
            // How many characters to write at a time.
            const int CHAR_BATCH_SIZE = 8192;

            // A big waste of memory. This ends up being 3x+ the length of the string, but we have to be safe. 
            int batchBufferSize = EncodingHelper.UTF8.GetMaxByteCount(CHAR_BATCH_SIZE);

            int numberOfBatches = responseBodyString.Length / CHAR_BATCH_SIZE;
            int remaining = responseBodyString.Length % CHAR_BATCH_SIZE;

            // Used to calculate when to set flush.
            bool noLeftoverCars = remaining == 0;

            for (int i = 0; i < numberOfBatches; i++)
            {
                WriteBatch(
                    responseBodyString.AsSpan().Slice(i * CHAR_BATCH_SIZE, CHAR_BATCH_SIZE), 
                    pipeWriter,
                    batchBufferSize,
                    encoder,
                    noLeftoverCars && i == numberOfBatches -1
                );
                
                await FlushPipe(pipeWriter);
            }

            if (remaining > 0)
            {
                WriteBatch(
                    responseBodyString.AsSpan().Slice(numberOfBatches * CHAR_BATCH_SIZE, remaining), 
                    pipeWriter,
                    batchBufferSize,
                    encoder,
                    true
                );
                 
                await FlushPipe(pipeWriter);
            }

            pipeWriter.Complete();
        }
    }
}