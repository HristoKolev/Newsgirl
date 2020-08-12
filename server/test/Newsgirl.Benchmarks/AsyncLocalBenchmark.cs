namespace Newsgirl.Benchmarks
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class AsyncLocalBenchmark
    {
        private static AsyncLocal<Func<Task<int>>> local;
        private static Func<Task<int>> localDirect;

        [Params(1_000_000)]
        public int N;

        private int simpleCount;
        private int asyncLocalCount;

        [GlobalSetup]
        public void GlobalSetup()
        {
            local = new AsyncLocal<Func<Task<int>>>();
        }

        [GlobalCleanup]
        public void GlobalCleanup() { }

        [Benchmark(Baseline = true)]
        public void ALCode()
        {
            this.DoALWork().GetAwaiter().GetResult();
        }

        private async Task DoALWork()
        {
            for (int i = 0; i < this.N; i++)
            {
                int num = 1;

#pragma warning disable 1998
                local.Value = async () =>
#pragma warning restore 1998
                {
                    return num;
                };

                await Task.Delay(0);
                await this.DoALWorkL2();

                num += this.asyncLocalCount;
            }
        }

        private async Task DoALWorkL2()
        {
            await Task.Delay(0);
            await this.DoALWorkL3();
        }

        private async Task DoALWorkL3()
        {
            await Task.Delay(0);
            this.asyncLocalCount += await local.Value();
        }

        [Benchmark]
        public void NormalWork()
        {
            this.DoNormalWork().GetAwaiter().GetResult();
        }

        private async Task DoNormalWork()
        {
            for (int i = 0; i < this.N; i++)
            {
                int num = 1;

                await Task.Delay(0);
                await this.DoNormalWorkL2();

                num += this.simpleCount;
            }
        }

        private async Task DoNormalWorkL2()
        {
            await Task.Delay(0);
            await this.DoNormalWorkL3();
        }

        private async Task DoNormalWorkL3()
        {
            await Task.Delay(0);
            this.simpleCount += 1;
        }

        [Benchmark]
        public void ClosureCode()
        {
            this.DoClosureWork().GetAwaiter().GetResult();
        }

        private async Task DoClosureWork()
        {
            for (int i = 0; i < this.N; i++)
            {
                int num = 1;

#pragma warning disable 1998
                localDirect = async () =>
#pragma warning restore 1998
                {
                    return num;
                };

                await Task.Delay(0);
                await this.DoClosureWorkL2();

                num += this.asyncLocalCount;
            }
        }

        private async Task DoClosureWorkL2()
        {
            await Task.Delay(0);
            await this.DoClosureWorkL3();
        }

        private async Task DoClosureWorkL3()
        {
            await Task.Delay(0);
            this.asyncLocalCount += await localDirect();
        }
    }
}
