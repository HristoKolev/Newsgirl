namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;

    /// <summary>
    /// Rents a byte[] of a given size from ArrayPool.Shared and returns it when disposed.
    /// </summary>
    public class RentedByteArrayHandle : IDisposable
    {
        public int Length { get; }

        private readonly byte[] buffer;

        public byte[] GetRentedArray() => this.buffer;

        public RentedByteArrayHandle(int length)
        {
            this.Length = length;
            this.buffer = ArrayPool<byte>.Shared.Rent(length);
        }

        public void Dispose() => ArrayPool<byte>.Shared.Return(this.buffer);

        public Span<byte> AsSpan() => new Span<byte>(this.buffer, 0, this.Length);

        public Memory<byte> AsMemory() => new Memory<byte>(this.buffer, 0, this.Length);
    }
}
