namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.IO;

    /// <summary>
    /// Acts as a handle to borrowed memory.
    /// </summary>
    public class RentedBuffer : IDisposable
    {
        public int Length { get; }

        private readonly byte[] buffer;

        private readonly MemoryStream memoryStream;

        public byte[] GetBuffer()
        {
            return this.buffer;
        }

        /// <summary>
        /// Rents a byte[] of a given size from ArrayPool.Shared and returns it when disposed.
        /// </summary>
        public RentedBuffer(int length)
        {
            this.Length = length;
            this.buffer = ArrayPool<byte>.Shared.Rent(length);
        }

        /// <summary>
        /// Takes a MemoryStream and disposes it when disposed.
        /// </summary>
        public RentedBuffer(MemoryStream memoryStream)
        {
            this.Length = (int) memoryStream.Length;
            this.buffer = memoryStream.GetBuffer();
            this.memoryStream = memoryStream;
        }

        public void Dispose()
        {
            if (this.memoryStream != null)
            {
                this.memoryStream.Dispose();
            }
            else
            {
                ArrayPool<byte>.Shared.Return(this.buffer);
            }
        }

        public Span<byte> AsSpan()
        {
            return new Span<byte>(this.buffer, 0, this.Length);
        }

        public Memory<byte> AsMemory()
        {
            return new Memory<byte>(this.buffer, 0, this.Length);
        }
    }
}
