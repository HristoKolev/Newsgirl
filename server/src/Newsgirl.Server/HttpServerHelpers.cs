namespace Newsgirl.Server
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared.Infrastructure;

    public static class HttpServerHelpers
    {
        /// <summary>
        ///     Writes a string in UTF-8 encoding and closes the stream.
        /// </summary>
        public static async ValueTask WriteUtf8(this HttpResponse response, string responseBodyString)
        {
            try
            {
                await using (var writer = new StreamWriter(response.Body, EncodingHelper.UTF8))
                {
                    await writer.WriteAsync(responseBodyString);
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
        ///     Reads the request stream to the end and decodes it into a string.
        /// </summary>
        public static async ValueTask<string> ReadUtf8(this HttpRequest request)
        {
            // ReSharper disable once PossibleInvalidOperationException
            int contentLength = (int) request.ContentLength.Value;

            // default pool
            var bufferPool = ArrayPool<byte>.Shared;
            var requestStream = request.Body;

            var requestBuffer = bufferPool.Rent(contentLength);

            try
            {
                int read;
                int offset = 0;

                while ((read = await requestStream.ReadAsync(requestBuffer, offset, contentLength - offset)) > 0)
                {
                    offset += read;
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
                        {"contentLength", contentLength}
                    }
                };
            }

            string requestBodyString;

            try
            {
                // Decode UTF8.
                requestBodyString = EncodingHelper.UTF8.GetString(requestBuffer.AsSpan(0, contentLength));
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

            return requestBodyString;
        }
    }
}
