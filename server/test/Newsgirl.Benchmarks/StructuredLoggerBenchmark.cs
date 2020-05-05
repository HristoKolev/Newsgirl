namespace Newsgirl.Benchmarks
{
    using System;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Shared;

    [MemoryDiagnoser]
    public class StructuredLoggerBenchmark
    {
        [Params(10_000_000)]
        public int N;

        private StructuredLogger<StructLogData> smallStructLogger;
        private StructuredLogger<ClassLogData> smallClassLogger;
        
        private StructuredLogger<LargeStructLogData> largeStructLogger;
        private StructuredLogger<LargeClassLogData> largeClassLogger;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.smallStructLogger = CreateLogger(new NoOpConsumer<StructLogData>(), LogLevel.Warn);
            this.smallClassLogger = CreateLogger(new NoOpConsumer<ClassLogData>(), LogLevel.Warn);
            this.largeStructLogger = CreateLogger(new NoOpConsumer<LargeStructLogData>(), LogLevel.Warn);
            this.largeClassLogger = CreateLogger(new NoOpConsumer<LargeClassLogData>(), LogLevel.Warn);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.smallStructLogger.DisposeAsync().GetAwaiter().GetResult();
            this.smallClassLogger.DisposeAsync().GetAwaiter().GetResult();
            this.largeStructLogger.DisposeAsync().GetAwaiter().GetResult();
            this.largeClassLogger.DisposeAsync().GetAwaiter().GetResult();
        }

        [Benchmark(Baseline = true)]
        public void NoLogging()
        {
            for (int i = 0; i < this.N; i++)
            {
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpStructData()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallStructLogger.Debug(() => new StructLogData());
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpClassData()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Debug(() => new ClassLogData());
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpStructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallStructLogger.Debug(() => new StructLogData { Number = i });
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Debug(() => new ClassLogData { Number = i });
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpConsumer_StructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallStructLogger.Warn(() => new StructLogData { Number = i });
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpConsumer_ClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Warn(() => new ClassLogData { Number = i });
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpConsumer_LargeStructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.largeStructLogger.Warn(() => new LargeStructLogData { Prop1 = i });
                int v = Math.Abs(i);
            }
        }
        
        [Benchmark]
        public void NoOpConsumer_LargeClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.largeClassLogger.Warn(() => new LargeClassLogData { Prop1 = i });
                int v = Math.Abs(i);
            }
        }

        private static StructuredLogger<T> CreateLogger<T>(LogConsumer<T> consumer, LogLevel logLevel)
        {
            return new StructuredLogger<T>(new[] { consumer }) { CurrentLevel = logLevel };
        }
    }

    internal class LargeClassLogData
    {
        public int Prop1 { get; set; }
        public int Prop2 { get; set; }
        public int Prop3 { get; set; }
        public int Prop4 { get; set; }
        public decimal Prop5 { get; set; }
        public decimal Prop6 { get; set; }
        public decimal Prop7 { get; set; }
        public decimal Prop8 { get; set; }
        public double Prop9 { get; set; }
        public double Prop10 { get; set; }
        public double Prop11 { get; set; }
        public double Prop12 { get; set; }
        public long Prop13 { get; set; }
        public long Prop14 { get; set; }
        public long Prop15 { get; set; }
        public long Prop16 { get; set; }
    }

    public struct LargeStructLogData
    {
        public int Prop1 { get; set; }
        public int Prop2 { get; set; }
        public int Prop3 { get; set; }
        public int Prop4 { get; set; }
        public decimal Prop5 { get; set; }
        public decimal Prop6 { get; set; }
        public decimal Prop7 { get; set; }
        public decimal Prop8 { get; set; }
        public double Prop9 { get; set; }
        public double Prop10 { get; set; }
        public double Prop11 { get; set; }
        public double Prop12 { get; set; }
        public long Prop13 { get; set; }
        public long Prop14 { get; set; }
        public long Prop15 { get; set; }
        public long Prop16 { get; set; }
    }

    public struct StructLogData
    {
        public int Number { get; set; }
    }

    public class ClassLogData
    {
        public int Number { get; set; }
    }

    public class NoOpConsumer<T> : LogConsumerBase<T>
    {
        protected override ValueTask ProcessBatch(ArraySegment<T> data)
        {
            return new ValueTask();
        }
    }
}
