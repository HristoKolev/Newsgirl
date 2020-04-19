using System;

namespace JsonTest
{
    using System.Buffers;
    using System.Diagnostics;
    using System.IO;
    using System.Text;
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

            while ((read = stream.Read(buffer, offset, (int)stream.Length - offset)) > 0)
            {
                offset += read;
            }

            using var doc = JsonDocument.Parse(buffer.AsMemory(0, offset));

            string type = doc.RootElement.GetProperty("type").GetString();

            Console.WriteLine(type);

            var model = JsonSerializer.Deserialize<MyModel>(buffer.AsSpan(0, offset));

            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
    
    public class MyModel
    {
        public MyModelHeader[] headers { get; set; }
        public string type { get; set; }
        public MyModelItem[] payload { get; set; }
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
