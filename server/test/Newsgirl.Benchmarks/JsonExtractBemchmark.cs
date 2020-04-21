namespace Newsgirl.Benchmarks
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text.Json;
    using BenchmarkDotNet.Attributes;
    using Testing;

    [MemoryDiagnoser]
    public class JsonExtractBenchmark
    {
        private byte[] data;

        [Params(100)]
        public int N;

        private Type wrapperType;
        private Dictionary<string, Type> requestTable;
        private Func<object, ConcreteWrapperObject> copyData;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();
            this.wrapperType = typeof(WrapperObject<>).MakeGenericType(typeof(ItemModel[]));
            
            var copyData = new DynamicMethod("copyData", typeof(ConcreteWrapperObject), new []{typeof(object)});

            var il = copyData.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(ConcreteWrapperObject).GetConstructors().First());
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(WrapperObject<>).MakeGenericType(typeof(object)).GetProperty("payload").GetMethod);
            il.Emit(OpCodes.Call, typeof(ConcreteWrapperObject).GetProperty("payload").SetMethod);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call, typeof(WrapperObject<>).MakeGenericType(typeof(object)).GetProperty("headers").GetMethod);
            il.Emit(OpCodes.Call, typeof(ConcreteWrapperObject).GetProperty("headers").SetMethod);
            il.Emit(OpCodes.Ret);
            
            this.copyData = (Func<object, ConcreteWrapperObject>)copyData.CreateDelegate(typeof(Func<object, ConcreteWrapperObject>));
            
            this.requestTable = new Dictionary<string, Type>
            {
                {"Req1", this.wrapperType}
            };
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
        }

        [Benchmark]
        public void DirectDeserialize()
        {
            for (int i = 0; i < this.N; i++)
            {
                // string typeName;
                //
                // using (var dom = JsonDocument.Parse(this.data))
                // {
                //     typeName = dom.RootElement.GetProperty("type").GetString();
                // }

                
                var model = JsonSerializer.Deserialize(this.data, this.wrapperType);
                GC.KeepAlive(model);
            }
        }
        
        [Benchmark]
        public void CreateDelegateTuple()
        {
            for (int i = 0; i < this.N; i++)
            {
                string typeName;
                
                using (var dom = JsonDocument.Parse(this.data))
                {
                    typeName = dom.RootElement.GetProperty("type").GetString();
                }

                if (!this.requestTable.TryGetValue(typeName, out var messageType))
                {
                    throw new NotImplementedException();
                }
                
                var wrapped = JsonSerializer.Deserialize(this.data, this.wrapperType);

                var real = this.copyData(wrapped);
                
                GC.KeepAlive(real);
            }
        }
        
        public class ConcreteWrapperObject
        {
            public Header[] headers { get; set; }
            
            public string type { get; set; }
            
            public object payload { get; set; }
        }
        
        public class WrapperObject<T>
        {
            public Header[] headers { get; set; }
            
            public string type { get; set; }
            
            public T payload { get; set; }
        }

        public class ItemModel
        {
            public string prop1 { get; set; }
            public string prop2 { get; set; }
            public string prop3 { get; set; }
            public string prop4 { get; set; }
            public string prop5 { get; set; }
        }

        public class Header
        {
            public string h1 { get; set; }
        }
    }
}
