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
    using System.Buffers;
    using System.Text.Json;
    using BenchmarkDotNet.Attributes;
    using Microsoft.Toolkit.HighPerformance.Buffers;

    [MemoryDiagnoser]
    public class JsonSizeCountBenchmark
    {
        [Params(1_000)]
        public int N;

        private MyClass tempInstance;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.tempInstance = new MyClass { vvv1 = new string('x', 100_000) };
        }

        [GlobalCleanup]
        public void GlobalCleanup() { }

        [Benchmark(Baseline = true)]
        public void NaiveImplementation()
        {
            for (int i = 0; i < this.N; i++)
            {
                _ = JsonSerializer.SerializeToUtf8Bytes(this.tempInstance).Length;
            }
        }

        [Benchmark]
        public void CountedBufferWriterImplementation()
        {
            for (int i = 0; i < this.N; i++)
            {
                var bufferWriter = new CountedBufferWriter();
                using (var utf8JsonWriter = new Utf8JsonWriter(bufferWriter))
                {
                    JsonSerializer.Serialize(utf8JsonWriter, this.tempInstance);
                }
            }
        }

        [Benchmark]
        public void ArrayPoolBufferWriterImplementation()
        {
            for (int i = 0; i < this.N; i++)
            {
                using (var bufferWriter = new ArrayPoolBufferWriter<byte>())
                using (var utf8JsonWriter = new Utf8JsonWriter(bufferWriter))
                {
                    JsonSerializer.Serialize(utf8JsonWriter, this.tempInstance);
                }
            }
        }
    }

    public class MyClass
    {
        public string vvv1 { get; set; }
    }

    public class CountedBufferWriter : IBufferWriter<byte>
    {
        private static readonly byte[] Buffer = new byte[1024000];

        public int Length { get; private set; }

        public void Advance(int count)
        {
            this.Length += count;
        }

        public Memory<byte> GetMemory(int sizeHint)
        {
            return new(Buffer);
        }

        public Span<byte> GetSpan(int sizeHint)
        {
            return new(Buffer);
        }
    }
}
