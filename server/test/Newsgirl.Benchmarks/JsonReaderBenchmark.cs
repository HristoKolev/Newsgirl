namespace Newsgirl.Benchmarks
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Text.Json;
    using BenchmarkDotNet.Attributes;
    using Shared.Infrastructure;
    using Testing;

    [MemoryDiagnoser]
    public class JsonReaderBenchmark
    {
        [Params(10, 100)]
        public int N;
        
        private byte[] data;
        private MemoryStream stream;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();
            this.stream = new MemoryStream(this.data);
        }
        
        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.stream.Dispose();
        }

        [Benchmark]
        public void JsonSerializer_DeserializeAsync()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.stream.Position = 0;
                
                JsonSerializer.DeserializeAsync<MyModel>(this.stream).GetAwaiter().GetResult();
            }
        }
        
        [Benchmark]
        public void JsonDocPlusDeserialize()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.stream.Position = 0;
                
                var buffer = ArrayPool<byte>.Shared.Rent((int) stream.Length);

                int read;
                int offset = 0;

                while ((read = stream.Read(buffer, offset, (int)stream.Length - offset)) > 0)
                {
                    offset += read;
                }

                using var doc = JsonDocument.Parse(buffer.AsMemory(0, offset));

                string type = doc.RootElement.GetProperty("type").GetString();

                var model = JsonSerializer.Deserialize<MyModel>(buffer.AsSpan(0, offset));

                ArrayPool<byte>.Shared.Return(buffer);
            }
        }

        [Benchmark]
        public void JsonSerializer_Deserialize_From_Reader()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.stream.Position = 0;
                
                var buffer = ArrayPool<byte>.Shared.Rent((int) this.stream.Length);

                int read;
                int offset = 0;

                while ((read = this.stream.Read(buffer, offset, (int)this.stream.Length - offset)) > 0)
                {
                    offset += read;
                }
            
                var reader = new Utf8JsonReader(buffer.AsSpan(0, offset));
            
                var model = JsonSerializer.Deserialize<MyModel>(ref reader);
            
                ArrayPool<byte>.Shared.Return(buffer);
            }
        }
        
        [Benchmark]
        public void JsonSerializer_Deserialize_From_Reader_Direct()
        {
            for (int i = 0; i < this.N; i++)
            {
                var reader = new Utf8JsonReader(this.data);

                var model = JsonSerializer.Deserialize<MyModel>(ref reader);
            }
        }
        
        [Benchmark]
        public void JsonNet_Deserialize()
        {
            for (int i = 0; i < this.N; i++)
            {
                this.stream.Position = 0;
                
                var buffer = ArrayPool<byte>.Shared.Rent((int) this.stream.Length);

                int read;
                int offset = 0;

                while ((read = this.stream.Read(buffer, offset, (int)this.stream.Length - offset)) > 0)
                {
                    offset += read;
                }
                
                string str = EncodingHelper.UTF8.GetString(buffer, 0, offset);

                var model = Newtonsoft.Json.JsonConvert.DeserializeObject<MyModel>(str);
                
                GC.KeepAlive(model);

                ArrayPool<byte>.Shared.Return(buffer);
            }
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
