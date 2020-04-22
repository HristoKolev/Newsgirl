namespace Newsgirl.Benchmarks
{
    using System;
    using System.Net.Http;
    using System.Threading.Tasks;
    using BenchmarkDotNet.Attributes;
    using Microsoft.AspNetCore.Http;
    using Server.Tests;
    using Shared;
    using Testing;

    [MemoryDiagnoser]
    public class HttpRequestReadingBenchmark
    {
        private HttpServerTester byteArrayTester;
        private HttpServerTester memoryTester;
        private ReadOnlyMemoryContent content;

        [Params(10_000)]
        public int N;

        [GlobalSetup]
        public void GlobalSetup()
        {
            this.byteArrayTester = HttpServerTester.Create(ByteArrayHandler).GetAwaiter().GetResult();
            this.memoryTester = HttpServerTester.Create(MemoryHandler).GetAwaiter().GetResult();
            
            var data = TestHelper.GetResourceBytes("../../../../../resources/large.json").GetAwaiter().GetResult();  
            
            this.content = new ReadOnlyMemoryContent(data);
        }

        [GlobalCleanup]
        public void GlobalCleanup()
        {
            this.byteArrayTester.DisposeAsync();
            this.memoryTester.DisposeAsync();
        }

#pragma warning disable 1998
        private static async Task MemoryHandler(HttpContext context)
#pragma warning restore 1998
        {
            try
            {
                // ReSharper disable once PossibleInvalidOperationException
                int contentLength = (int) context.Request.ContentLength.Value;
            
                 var bufferHandle = new RentedByteArrayHandle(contentLength);
            
                while (await context.Request.Body.ReadAsync(bufferHandle.AsMemory()) > 0) { }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }

#pragma warning disable 1998
        private static async Task ByteArrayHandler(HttpContext context)
#pragma warning restore 1998
        {
            // try
            // {
            //     // ReSharper disable once PossibleInvalidOperationException
            //     int contentLength = (int) context.Request.ContentLength.Value;
            //
            //     using var bufferHandle = new PolledBufferHandle(contentLength);
            //
            //     int read;
            //     int offset = 0;
            //
            //     while ((read = await context.Request.Body.ReadAsync(bufferHandle.Buffer, offset, contentLength - offset)) >
            //            0)
            //     {
            //         offset += read;
            //     }
            // }
            // catch (Exception e)
            // {
            //     Console.WriteLine(e);
            //     throw;
            // }
        }

        [Benchmark]
        public void ByteArrayRead()
        {
            try
            {
                for (int i = 0; i < this.N; i++)
                {
                    this.byteArrayTester.Client.PostAsync("/", this.content).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
        
        [Benchmark]
        public void MemoryRead()
        {
            try
            {
                for (int i = 0; i < this.N; i++)
                {
                    this.memoryTester.Client.PostAsync("/", this.content).GetAwaiter().GetResult();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }
}
