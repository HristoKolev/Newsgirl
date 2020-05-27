namespace Newsgirl.Server
{
    using System;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Microsoft.IO;
    using Shared;

    public static class HttpServerHelpers
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        /// <summary>
        ///     Writes a string in UTF-8 encoding and closes the stream.
        /// </summary>
        public static async ValueTask WriteUtf8(this HttpResponse response, string str)
        {
            try
            {
                await using (var writer = new StreamWriter(response.Body, EncodingHelper.UTF8))
                {
                    await writer.WriteAsync(str);
                }
            }
            catch (EncoderFallbackException err)
            {
                throw new DetailedLogException("Failed to encode UTF8 response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY"
                };
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write to HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_TO_RESPONSE_BODY"
                };
            }
        }

        /// <summary>
        ///     Reads the request stream to the end and decodes it into a <see cref="string" />.
        ///     Throws on invalid UTF8.
        /// </summary>
        public static async ValueTask<string> ReadUtf8(this HttpRequest request)
        {
            using var requestContent = await request.ReadToEnd();

            string requestBodyString;

            try
            {
                // Decode UTF8.
                requestBodyString = EncodingHelper.UTF8.GetString(requestContent.AsSpan());
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to decode UTF8 from HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_DECODE_UTF8_REQUEST_BODY",
                    Details =
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestContent.AsSpan())}
                    }
                };
            }

            return requestBodyString;
        }

        /// <summary>
        ///     Reads the request stream to the end and returns <see cref="RentedByteArrayHandle" /> with the contents.
        /// </summary>
        public static async ValueTask<RentedByteArrayHandle> ReadToEnd(this HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                var bufferHandle = new RentedByteArrayHandle((int) request.ContentLength.Value);

                try
                {
                    int read;
                    int offset = 0;

                    var buffer = bufferHandle.GetRentedArray();

                    while ((read = await request.Body.ReadAsync(buffer, offset, bufferHandle.Length - offset)) > 0)
                    {
                        offset += read;
                    }
                }
                catch (Exception err)
                {
                    int length = bufferHandle.Length;
                    bufferHandle.Dispose();

                    throw new DetailedLogException("Failed to read the HTTP request body.", err)
                    {
                        Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                        Details =
                        {
                            {"contentLength", length}
                        }
                    };
                }

                return bufferHandle;
            }
            
            var memoryStream = MemoryStreamManager.GetStream();
            
            try
            {
                await request.Body.CopyToAsync(memoryStream);
            }
            catch (Exception err)
            {
                long length = memoryStream.Length;
                // ReSharper disable once MethodHasAsyncOverload
                memoryStream.Dispose();

                throw new DetailedLogException("Failed to read the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                    Details =
                    {
                        {"contentLength", length}
                    }
                };
            }

            return new RentedByteArrayHandle(memoryStream);
        }
    }
}
