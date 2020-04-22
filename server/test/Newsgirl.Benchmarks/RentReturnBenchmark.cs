namespace Newsgirl.Benchmarks
{
    using System;
    using System.Buffers;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Shared;

    [MemoryDiagnoser]
    public class RentReturnBenchmark
    {
        [Params(1_000_000)]
        public int N;

        [Params(1024)]
        public int Size;

        [GlobalSetup]
        public void GlobalSetup()
        {
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [Benchmark]
        public void SimpleCase()
        {
            for (int i = 0; i < this.N; i++)
            {
                var buffer = ArrayPool<byte>.Shared.Rent(this.Size);

                GC.KeepAlive(buffer);

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        
        [Benchmark]
        public void AsyncSimpleCase()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.AsyncSimpleCase_Impl().GetAwaiter().GetResult();
            }
        }
        
        private async Task AsyncSimpleCase_Impl()
        {
            var buffer = ArrayPool<byte>.Shared.Rent(this.Size);
            
            await Task.Delay(0);
            
            ArrayPool<byte>.Shared.Return(buffer);
        }

        [Benchmark]
        public void ArrayPoolBuffer()
        {
            for (int i = 0; i < this.N; i++)
            {
                using var bufferHolder = new RentedByteArrayHandle(this.Size);

                GC.KeepAlive(bufferHolder.GetRentedArray());
            }
        }
        
        [Benchmark]
        public void AsyncArrayPoolBuffer()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.AsyncArrayPoolBuffer_Impl().GetAwaiter().GetResult();
            }
        }
        
        private async Task AsyncArrayPoolBuffer_Impl()
        {
            using var bufferHolder = new RentedByteArrayHandle(this.Size);
            
            await Task.Delay(0);
            
            GC.KeepAlive(bufferHolder.GetRentedArray());
        }
    }
}
