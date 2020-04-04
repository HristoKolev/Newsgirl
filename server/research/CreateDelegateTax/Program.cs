using System;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

namespace CreateDelegateTax
{
    public static class TestTarget
    {
        public static long Cats = 0;

        public static void Direct()
        {
            Cats += 1;
        }
        
        public static void Indirect()
        {
            Cats += 1;
        }
    }
    
    public class CreateDelegateTaxBenchmark
    {
        private readonly Action indirectDelegate;
        private readonly Action directDelegate;
        
        public CreateDelegateTaxBenchmark()
        {
            this.indirectDelegate = (Action) typeof(TestTarget).GetMethod("Indirect").CreateDelegate(typeof(Action));
            this.directDelegate = TestTarget.Direct;
        }

        [Benchmark]
        public void Direct() => this.directDelegate();

        [Benchmark]
        public void Indirect() => this.indirectDelegate();
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var summary = BenchmarkRunner.Run<CreateDelegateTaxBenchmark>();
        }
    }
}