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
            
            var fn = (Func<object, ConcreteWrapperObject>)copyData.CreateDelegate(typeof(Func<object, ConcreteWrapperObject>));
            
            var obj = new WrapperObject<ItemModel[]>()
            {
                headers = new Header[10],
                payload = new ItemModel[10],
            };

            var res = fn(obj);

            Console.WriteLine(res);
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
