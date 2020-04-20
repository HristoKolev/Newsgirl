namespace Newsgirl.Benchmarks
{
    using System;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text.Json;
    using BenchmarkDotNet.Attributes;
    using Testing;

    [MemoryDiagnoser]
    public class JsonExtractBenchmark
    {
        private byte[] data;

        [Params(1_000_000)]
        public int N;

        private Func<object, object> getPayload;
        private object model;
        private Func<object, MyModelHeader[]> getHeaders;
        private Func<object, (object, MyModelHeader[])> getCombined;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();
            
            var getPayloadMethod = new DynamicMethod("getPayloadMethod", typeof(object), new []{typeof(object)});

            var getPayloadGen = getPayloadMethod.GetILGenerator();
            getPayloadGen.Emit(OpCodes.Ldarg_0);
            getPayloadGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("payload").GetMethod);
            getPayloadGen.Emit(OpCodes.Ret);
            
            this.getPayload = (Func<object, object>)getPayloadMethod.CreateDelegate(typeof(Func<object, object>));
            
            var getHeadersMethod = new DynamicMethod("getPayloadMethod", typeof(MyModelHeader[]), new []{typeof(object)});

            var getHeadersGen = getHeadersMethod.GetILGenerator();
            getHeadersGen.Emit(OpCodes.Ldarg_0);
            getHeadersGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("headers").GetMethod);
            getHeadersGen.Emit(OpCodes.Ret);
            
            this.getHeaders = (Func<object, MyModelHeader[]>)getHeadersMethod.CreateDelegate(typeof(Func<object, MyModelHeader[]>));
            
            
            
            
            
            
            
            var getCombinedMethod = new DynamicMethod("getPayloadMethod", typeof((object, MyModelHeader[])), new []{typeof(object)});

            var getCombinedGen = getCombinedMethod.GetILGenerator();
            getCombinedGen.Emit(OpCodes.Ldarg_0);
            getCombinedGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("payload").GetMethod);
            getCombinedGen.Emit(OpCodes.Ldarg_0);
            getCombinedGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("headers").GetMethod);
            getCombinedGen.Emit(OpCodes.Newobj, typeof(ValueTuple<object, MyModelHeader[]>).GetConstructors().First());
            getCombinedGen.Emit(OpCodes.Ret);
            
            this.getCombined = (Func<object, (object, MyModelHeader[])>)getCombinedMethod.CreateDelegate(typeof(Func<object, (object, MyModelHeader[])>));

            var wrapperType = typeof(MyWrapper<>).MakeGenericType(typeof(MyModelItem[]));

            this.model = JsonSerializer.Deserialize(this.data, wrapperType);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [Benchmark]
        public void CreateDelegate()
        {
            for (int i = 0; i < this.N; i++)
            {
                var payload = this.getPayload(this.model);
                var headers = this.getHeaders(this.model);
                GC.KeepAlive(payload);
                GC.KeepAlive(headers);
            }
        }
        
        [Benchmark]
        public void CreateDelegateTuple()
        {
            for (int i = 0; i < this.N; i++)
            {
                var (payload, headers) = this.getCombined(this.model);
                GC.KeepAlive(payload);
                GC.KeepAlive(headers);
            }
        }
        
        [Benchmark]
        public void InvariantCast()
        {
            for (int i = 0; i < this.N; i++)
            {
                var m = (IMyWrapper<object>)this.model;
                var payload = m.payload;
                var headers = m.headers;

                GC.KeepAlive(payload);
                GC.KeepAlive(headers);
            }
        }

        public interface IMyWrapper<out T>
        {
            MyModelHeader[] headers { get; set; }
            
            string type { get; set; }
            
            T payload { get; }
        }

        public class MyWrapper<T> : IMyWrapper<T>
        {
            public MyModelHeader[] headers { get; set; }
            
            public string type { get; set; }
            
            public T payload { get; set; }
        }

        public class MyModelItem
        {
            public string prop1 { get; set; }
            public string prop2 { get; set; }
            public string prop3 { get; set; }
            public string prop4 { get; set; }
            public string prop5 { get; set; }
        }

        public class MyModelHeader
        {
            public string h1 { get; set; }
        }
    }
}
