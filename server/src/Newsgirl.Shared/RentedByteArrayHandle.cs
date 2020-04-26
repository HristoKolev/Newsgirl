namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;

    /// <summary>
    /// Rents a byte[] of a given size from ArrayPool.Shared and returns it when disposed.
    /// </summary>
    public readonly struct RentedByteArrayHandle : IDisposable
    {
        public readonly int Length;
        
        private readonly byte[] buffer;

        public byte[] GetRentedArray()
        {
            return this.buffer;
        }

        public RentedByteArrayHandle(int length)
        {
            this.Length = length;
            this.buffer = ArrayPool<byte>.Shared.Rent(length);
        }

        public bool HasData => this.buffer != null;

        public void Dispose()
        {
            ArrayPool<byte>.Shared.Return(this.buffer);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Span<byte> AsSpan()
        {
            return new Span<byte>(this.buffer, 0, this.Length);
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Memory<byte> AsMemory()
        {
            return new Memory<byte>(this.buffer, 0, this.Length);
        }
    }
}
