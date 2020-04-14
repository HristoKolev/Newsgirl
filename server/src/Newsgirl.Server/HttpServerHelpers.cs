using System;
using System.IO.Pipelines;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Server
{
    public static class HttpServerHelpers
    {
        // How many characters to write at a time.
        private const int WriteUtf8DefaultCharBatchSize = 8192;

        public static async ValueTask WriteUtf8(this HttpResponse response, string responseBodyString, int? batchSize = null)
        {
            var pipeWriter = response.BodyWriter;

            // EncoderNLS is or at least should be stateful.
            // +1 allocation.
            var encoder = EncodingHelper.UTF8.GetEncoder();

            // How many characters to write at a time.
            int charBatchSize = batchSize ?? WriteUtf8DefaultCharBatchSize;

            // A big waste of memory. This ends up being 3x+ the length of the string, but we have to be safe. 
            int batchBufferSize = EncodingHelper.UTF8.GetMaxByteCount(charBatchSize);

            int numberOfBatches = responseBodyString.Length / charBatchSize;
            int leftoverCharCount = responseBodyString.Length % charBatchSize;

            for (int i = 0; i < numberOfBatches; i++)
            {
                WriteBatch(
                    responseBodyString.AsSpan().Slice(i * charBatchSize, charBatchSize),
                    pipeWriter,
                    batchBufferSize,
                    encoder,
                    leftoverCharCount == 0 && i == numberOfBatches - 1
                );

                await FlushPipe(pipeWriter);
            }

            if (leftoverCharCount != 0)
            {
                WriteBatch(
                    responseBodyString.AsSpan().Slice(numberOfBatches * charBatchSize, leftoverCharCount),
                    pipeWriter,
                    batchBufferSize,
                    encoder,
                    true
                );

                await FlushPipe(pipeWriter);
            }

            pipeWriter.Complete();
        }

        private static void WriteBatch(
            ReadOnlySpan<char> batchCharacters,
            PipeWriter bodyWriter,
            int batchBufferSize,
            Encoder encoder,
            bool flush)
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
    }
}