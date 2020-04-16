using System;
using System.Diagnostics;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
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
        
        public static async Task Run()
        {
            int n = 1000;
            bool readResponse = false;

            await using (var tester = await HttpServerTester.Create(Handler))
            {
                byte[] utf8 = await TestHelper.GetResourceBytes("unicode_test_page.html");

                int byteRepeats = 4;
                
                payloadBytes = new byte[utf8.Length * byteRepeats];

                for (int i = 0; i < byteRepeats; i++)
                {
                    Buffer.BlockCopy(utf8, 0, payloadBytes, utf8.Length * i, utf8.Length);
                }

                payloadString = EncodingHelper.UTF8.GetString(payloadBytes);

                var content = new ReadOnlyMemoryContent(payloadBytes);
            
                var responses = new HttpResponseMessage[n];
                
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true);
                GC.WaitForPendingFinalizers();
                
                int gen0Before = GC.CollectionCount(0);
                int gen1Before = GC.CollectionCount(1);
                int gen2Before = GC.CollectionCount(2);

                var sw = Stopwatch.StartNew();
                
                for (int i = 0; i < n; i++)
                {
                    if (readResponse)
                    {
                        responses[i] = await tester.Client.PostAsync("/", content);
                    }
                    else
                    {
                        var response = await tester.Client.PostAsync("/", content);
                        responses[i] = response;

                        await response.Content.ReadAsByteArrayAsync();
                    }
                }
                
                int gen0After = GC.CollectionCount(0);
                int gen1After = GC.CollectionCount(1);
                int gen2After = GC.CollectionCount(2);

                Console.WriteLine($"TIME: {sw.ElapsedMilliseconds}");
                Console.WriteLine($"GEN0: {gen0After - gen0Before}; GEN1: {gen1After - gen1Before}; GEN2: {gen2After - gen2Before};");
            }
        }

        private static async Task Handler(HttpContext context)
        {
            // string tmp = await context.Request.ReadUtf8();

            // using (var streamReader = new StreamReader(context.Request.Body, EncodingHelper.UTF8))
            // {
            //     string tmp = await streamReader.ReadToEndAsync();
            // }

            context.Response.StatusCode = 200;
            
//             return Task.CompletedTask;
            //
            // await using (var streamWriter = new StreamWriter(context.Response.Body, EncodingHelper.UTF8))
            // {
            //     await streamWriter.WriteAsync(payloadString);
            // }

            await context.Response.WriteUtf8(payloadString);
        }
    }
}