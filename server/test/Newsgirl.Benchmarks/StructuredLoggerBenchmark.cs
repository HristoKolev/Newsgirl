// ReSharper disable InconsistentNaming
// ReSharper disable UnusedVariable
// ReSharper disable UnusedMember.Global
// ReSharper disable AccessToModifiedClosure
// ReSharper disable MemberCanBePrivate.Global
// ReSharper disable UnassignedField.Global
// ReSharper disable NotAccessedVariable
// ReSharper disable RedundantAssignment

namespace Newsgirl.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Shared.Logging;

    [MemoryDiagnoser]
    public class StructuredLoggerBenchmark
    {
        private const string DebugConfig = "DebugConfig";
        private const string WarnConfig = "WarnConfig";
        private const string NoopConsumerName = "WarnConfig";

        [Params(10_000_000)]
        public int N;

        private StructuredLogger smallStructLogger;
        private StructuredLogger smallClassLogger;

        private StructuredLogger largeStructLogger;
        private StructuredLogger largeClassLogger;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.smallStructLogger = CreateLogger(new NoOpConsumer<StructLogData>());
            this.smallClassLogger = CreateLogger(new NoOpConsumer<ClassLogData>());
            this.largeStructLogger = CreateLogger(new NoOpConsumer<LargeStructLogData>());
            this.largeClassLogger = CreateLogger(new NoOpConsumer<LargeClassLogData>());
        }

        private static StructuredLogger CreateLogger<T>(NoOpConsumer<T> consumer)
        {
            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(WarnConfig, new Dictionary<string, Func<EventDestination<T>>>
            {
                { NoopConsumerName, () => consumer },
            });

            var logger = builder.Build();

            logger.Reconfigure(new[]
            {
                new EventStreamConfig
                {
                    Name = WarnConfig,
                    Enabled = true,
                    Destinations = new[]
                    {
                        new EventDestinationConfig
                        {
                            Name = NoopConsumerName,
                            Enabled = true,
                        },
                    },
                },
            });

            return logger;
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
                this.smallStructLogger.Log(DebugConfig, () => new StructLogData());
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpClassData()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Log(DebugConfig, () => new ClassLogData());
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpStructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallStructLogger.Log(DebugConfig, () => new StructLogData { Number = i });
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Log(DebugConfig, () => new ClassLogData { Number = i });
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpConsumer_StructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallStructLogger.Log(WarnConfig, () => new StructLogData { Number = i });
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpConsumer_ClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.smallClassLogger.Log(WarnConfig, () => new ClassLogData { Number = i });
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpConsumer_LargeStructData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.largeStructLogger.Log(WarnConfig, () => new LargeStructLogData { Prop1 = i });
                int v = Math.Abs(i);
            }
        }

        [Benchmark]
        public void NoOpConsumer_LargeClassData_Closure()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.largeClassLogger.Log(WarnConfig, () => new LargeClassLogData { Prop1 = i });
                int v = Math.Abs(i);
            }
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

    public class NoOpConsumer<T> : EventDestination<T>
    {
        protected override ValueTask Flush(ArraySegment<T> data)
        {
            return new ValueTask();
        }

        public NoOpConsumer() : base(null) { }
    }
}
