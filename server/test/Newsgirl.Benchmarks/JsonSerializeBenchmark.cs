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
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Microsoft.AspNetCore.Http;
    using Server.Tests.Infrastructure;
    using Testing;

    [MemoryDiagnoser]
    public class JsonSerializeBenchmark
    {
        [Params(100)]
        public int N;

        private object model;
        private JsonSerializerOptions jsonSerializerOptions;
        private HttpServerTester streamTester;
        private HttpServerTester pipeTester;

        [GlobalSetup]
        public void GlobalSetup()
        {
            var data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();
            this.model = JsonSerializer.Deserialize<MyModel>(data);
            this.jsonSerializerOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            };

            this.streamTester = HttpServerTester.Create(this.StreamHandler).GetAwaiter().GetResult();
            this.pipeTester = HttpServerTester.Create(this.PipeHandler).GetAwaiter().GetResult();
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.streamTester.DisposeAsync();
            this.pipeTester.DisposeAsync();
        }

        private async Task PipeHandler(HttpContext context)
        {
            try
            {
                for (int i = 0; i < this.N; i++)
                {
                    await using var w = new Utf8JsonWriter(context.Response.BodyWriter);
                    JsonSerializer.Serialize(w, this.model, typeof(MyModel), this.jsonSerializerOptions);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        private async Task StreamHandler(HttpContext context)
        {
            try
            {
                for (int i = 0; i < this.N; i++)
                {
                    await JsonSerializer.SerializeAsync(context.Response.Body, this.model, typeof(MyModel), this.jsonSerializerOptions);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Benchmark(Baseline = true)]
        public void PipeWrite()
        {
            try
            {
                this.pipeTester.Client.GetAsync("/").GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        [Benchmark]
        public void StreamWrite()
        {
            try
            {
                this.streamTester.Client.GetAsync("/").GetAwaiter().GetResult();
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

        public class MyModel
        {
            public Dictionary<string, string> headers { get; set; }

            public string type { get; set; }

            public MyItem[] payload { get; set; }
        }

        public class MyItem
        {
            public string prop1 { get; set; }
            public string prop2 { get; set; }
            public string prop3 { get; set; }
            public string prop4 { get; set; }
            public string prop5 { get; set; }
        }
    }
}
