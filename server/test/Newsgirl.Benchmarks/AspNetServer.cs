using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.WebUtilities;
using Newsgirl.Server;
using Newsgirl.Server.Tests;
using Newsgirl.Shared.Infrastructure;
using Newsgirl.Testing;

namespace Newsgirl.Benchmarks
{
    public static class AspNetServer
    {
        static byte[] payloadBytes;
        static string payloadString;
        
        private const int N = 1_000;

        public static async Task Run()
        {
            await TestSetup();
            
            await StreamWriterTest();
            await HttpResponseStreamWriterTest();
            await WriteUtf8Test();

            Console.WriteLine("==========");

            await StreamWriterTest();
            await HttpResponseStreamWriterTest();
            await WriteUtf8Test();

        }

        private static async Task TestSetup()
        {
            byte[] utf8 = await TestHelper.GetResourceBytes("unicode_test_page.html");

            int byteRepeats = 4;

            payloadBytes = new byte[utf8.Length * byteRepeats];

            for (int i = 0; i < byteRepeats; i++)
            {
                Buffer.BlockCopy(utf8, 0, payloadBytes, utf8.Length * i, utf8.Length);
            }

            payloadString = EncodingHelper.UTF8.GetString(payloadBytes);
        }

        private static async Task WriteUtf8Test()
        {
            static async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;
                await context.Response.WriteUtf8(payloadString);
            }
            
            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var content = new ReadOnlyMemoryContent(payloadBytes);
            
                var responses = new HttpResponseMessage[N];
                
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                
                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();
                
                for (int i = 0; i < N; i++)
                {
                    var response = await tester.Client.PostAsync("/", content);
                    responses[i] = response;

                  //  byte[]dd  = await response.Content.ReadAsByteArrayAsync();

                }

                int gen0After = GC.CollectionCount(0);
                int gen1After = GC.CollectionCount(1);
                int gen2After = GC.CollectionCount(2);

                Console.WriteLine($"WriteUtf8Test: TIME: {sw.ElapsedMilliseconds}");
                Console.WriteLine($"WriteUtf8Test: GEN0: {gen0After - gen0Before}; GEN1: {gen1After - gen1Before}; GEN2: {gen2After - gen2Before};");
            }
        }
        
        private static async Task StreamWriterTest()
        {
            static async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;

                await using (var streamWriter = new StreamWriter(context.Response.Body, EncodingHelper.UTF8))
                {
                    await streamWriter.WriteAsync(payloadString);
                }
            }
            
            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var content = new ReadOnlyMemoryContent(payloadBytes);
            
                var responses = new HttpResponseMessage[N];
                
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                
                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();
                
                for (int i = 0; i < N; i++)
                {
                    var response = await tester.Client.PostAsync("/", content);
                    responses[i] = response;


                   // byte[] dd = await response.Content.ReadAsByteArrayAsync();

                }

                int gen0After = GC.CollectionCount(0);
                int gen1After = GC.CollectionCount(1);
                int gen2After = GC.CollectionCount(2);

                Console.WriteLine($"StreamWriterTest: TIME: {sw.ElapsedMilliseconds}");
                Console.WriteLine($"StreamWriterTest: GEN0: {gen0After - gen0Before}; GEN1: {gen1After - gen1Before}; GEN2: {gen2After - gen2Before};");
            }
        }

        private static async Task HttpResponseStreamWriterTest()
        {
            static async Task Handler(HttpContext context)
            {
                context.Response.StatusCode = 200;

                await using (var streamWriter = new HttpResponseStreamWriter(context.Response.Body, EncodingHelper.UTF8))
                {
                    await streamWriter.WriteAsync(payloadString);
                }
            }

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                var content = new ReadOnlyMemoryContent(payloadBytes);

                var responses = new HttpResponseMessage[N];

                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();

                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();

                for (int i = 0; i < N; i++)
                {
                    var response = await tester.Client.PostAsync("/", content);
                    responses[i] = response;


                    // byte[] dd = await response.Content.ReadAsByteArrayAsync();

                }

                int gen0After = GC.CollectionCount(0);
                int gen1After = GC.CollectionCount(1);
                int gen2After = GC.CollectionCount(2);

                Console.WriteLine($"HttpResponseStreamWriterTest: TIME: {sw.ElapsedMilliseconds}");
                Console.WriteLine($"HttpResponseStreamWriterTest: GEN0: {gen0After - gen0Before}; GEN1: {gen1After - gen1Before}; GEN2: {gen2After - gen2Before};");
            }
        }
    }
}