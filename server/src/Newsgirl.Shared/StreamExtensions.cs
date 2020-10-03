namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.HighPerformance.Buffers;

    public static class StreamExtensions
    {
        public static async ValueTask<IMemoryOwner<byte>> ReadUnknownSizeStream(this Stream source)
        {
            var buffer = new ArrayPoolBufferWriter<byte>();

            try
            {
                int bytesRead;

                while ((bytesRead = await source.ReadAsync(buffer.GetMemory(8192))) != 0)
                {
                    buffer.Advance(bytesRead);
                }

                return buffer;
            }
            catch (Exception)
            {
                buffer.Dispose();
                throw;
            }
        }
    }
}
