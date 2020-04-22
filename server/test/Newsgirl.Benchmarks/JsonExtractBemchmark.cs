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
        private Func<object, ConcreteWrapperObject> copyData;
        private byte[] data;

        [Params(100)]
        public int N;

        private Dictionary<string, Type> requestTable;
        private JsonSerializerOptions serializationOptions;

        private Type wrapperType;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();
            this.wrapperType = typeof(WrapperObject<>).MakeGenericType(typeof(ItemModel[]));

            var copyData = new DynamicMethod("copyData", typeof(ConcreteWrapperObject), new[] {typeof(object)});

            var il = copyData.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(ConcreteWrapperObject).GetConstructors().First());
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call,
                typeof(WrapperObject<>).MakeGenericType(typeof(object)).GetProperty("payload").GetMethod);
            il.Emit(OpCodes.Call, typeof(ConcreteWrapperObject).GetProperty("payload").SetMethod);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Call,
                typeof(WrapperObject<>).MakeGenericType(typeof(object)).GetProperty("headers").GetMethod);
            il.Emit(OpCodes.Call, typeof(ConcreteWrapperObject).GetProperty("headers").SetMethod);
            il.Emit(OpCodes.Ret);

            this.copyData =
                (Func<object, ConcreteWrapperObject>) copyData.CreateDelegate(
                    typeof(Func<object, ConcreteWrapperObject>));

            this.requestTable = new Dictionary<string, Type>
            {
                {"Req1", this.wrapperType}
            };

            this.serializationOptions = new JsonSerializerOptions {PropertyNameCaseInsensitive = true};
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
                var model = JsonSerializer.Deserialize(this.data, this.wrapperType, this.serializationOptions);
                GC.KeepAlive(model);
            }
        }

        [Benchmark(Baseline = true)]
        public void DomPlusDeserialize()
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

                var wrapped = JsonSerializer.Deserialize(this.data, messageType, this.serializationOptions);

                var real = this.copyData(wrapped);

                GC.KeepAlive(real);
            }
        }

        [Benchmark]
        public void DeserializeTwice()
        {
            for (int i = 0; i < this.N; i++)
            {
                var d1 = JsonSerializer.Deserialize<TypeOnlyModel>(this.data, this.serializationOptions);

                if (!this.requestTable.TryGetValue(d1.type, out var messageType))
                {
                    throw new NotImplementedException();
                }

                var wrapped = JsonSerializer.Deserialize(this.data, messageType, this.serializationOptions);

                var real = this.copyData(wrapped);

                GC.KeepAlive(real);
            }
        }

        public class ConcreteWrapperObject
        {
            public Dictionary<string, string> headers { get; set; }

            public string type { get; set; }

            public object payload { get; set; }
        }

        public struct TypeOnlyModel
        {
            public string type { get; set; }
        }

        public class WrapperObject<T>
        {
            public Dictionary<string, string> headers { get; set; }

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
    }
}
