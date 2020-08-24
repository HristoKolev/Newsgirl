// ReSharper disable InconsistentNaming

namespace Newsgirl.Benchmarks
{
    using System;
    using System.Collections.Concurrent;
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class TypeCacheBenchmark
    {
        [Params(1_000_000)]
        public int N;

        private ConcurrentDictionary<Type, Type> table;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.table = new ConcurrentDictionary<Type, Type>();
        }

        [GlobalCleanup]
        public void GlobalCleanup() { }

        [Benchmark]
        public void AwaysCreate()
        {
            for (int i = 0; i < this.N; i++)
            {
                var t = typeof(WrapperObject<>).MakeGenericType(typeof(string));
                GC.KeepAlive(t);
            }
        }

        [Benchmark]
        public void CacheInConcurrentDictionary()
        {
            for (int i = 0; i < this.N; i++)
            {
                var t = this.table.GetOrAdd(typeof(string), ValueFactory);
                GC.KeepAlive(t);
            }
        }

        private static Type ValueFactory(Type arg)
        {
            return typeof(WrapperObject<>).MakeGenericType(arg);
        }

        public class WrapperObject<T>
        {
            public T payload { get; set; }
        }
    }
}
