using System;

namespace JsonTest
{
    using System.Buffers;
    using System.IO;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text.Json;

    class Program
    {
        static void Main(string[] args)
        {
            var data = File.ReadAllBytes(
                "/work/projects/Newsgirl/server/test/Newsgirl.Benchmarks/resources/large.json");
            var stream = new MemoryStream(data);
           
            var buffer = ArrayPool<byte>.Shared.Rent((int) stream.Length);

            int read;
            int offset = 0;

            while ((read = stream.Read(buffer, offset, (int) stream.Length - offset)) > 0)
            {
                offset += read;
            }

            using var doc = JsonDocument.Parse(buffer.AsMemory(0, offset));

            string requestType = doc.RootElement.GetProperty("type").GetString();

            var wrapperType = typeof(MyWrapper<>).MakeGenericType(typeof(MyModelItem[]));

            var model =   JsonSerializer.Deserialize(buffer.AsSpan(0, offset), wrapperType);

            var getCombinedMethod = new DynamicMethod("getPayloadMethod", typeof((object, MyModelHeader[])), new []{typeof(object)});

            var getCombinedGen = getCombinedMethod.GetILGenerator();
            getCombinedGen.Emit(OpCodes.Ldarg_0);
            getCombinedGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("payload").GetMethod);
            getCombinedGen.Emit(OpCodes.Ldarg_0);
            getCombinedGen.Emit(OpCodes.Call, typeof(MyWrapper<>).MakeGenericType(typeof(object)).GetProperty("headers").GetMethod);
            getCombinedGen.Emit(OpCodes.Newobj, typeof(ValueTuple<object, MyModelHeader[]>).GetConstructors().First());
            getCombinedGen.Emit(OpCodes.Ret);
            
            var getCombined = (Func<object, (object, MyModelHeader[])>)getCombinedMethod.CreateDelegate(typeof(Func<object, (object, MyModelHeader[])>));


            var (payload, headers) = getCombined(model);
            
            GC.KeepAlive(payload);
            GC.KeepAlive(headers);

            ArrayPool<byte>.Shared.Return(buffer);
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
