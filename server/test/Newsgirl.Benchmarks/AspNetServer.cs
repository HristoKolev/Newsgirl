namespace Newsgirl.Benchmarks
{
    using System;
    using System.Diagnostics;
    using System.IO;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Server.Tests;
    using Shared.Infrastructure;
    using Testing;

    public static class AspNetServer
    {
        private const int N = 1000;
        private static string payloadString;

        public static async Task Run()
        {
            await TestSetup();

            await StreamWriterTest();
        }

        private static async Task TestSetup()
        {
            var utf8 = await TestHelper.GetResourceBytes("unicode_test_page.html");

            int byteRepeats = 4;

            var payloadBytes = new byte[utf8.Length * byteRepeats];

            for (int i = 0; i < byteRepeats; i++)
            {
                Buffer.BlockCopy(utf8, 0, payloadBytes, utf8.Length * i, utf8.Length);
            }

            payloadString = EncodingHelper.UTF8.GetString(payloadBytes);
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
                var sw = Stopwatch.StartNew();

                for (int i = 0; i < N; i++)
                {
                    await tester.Client.GetAsync("/");
                }

                sw.Stop();

                int gen0Passes = GC.CollectionCount(0);
                int gen1Passes = GC.CollectionCount(1);
                int gen2Passes = GC.CollectionCount(2);

                Console.WriteLine($"StreamWriterTest: TIME: {sw.ElapsedMilliseconds}");
                Console.WriteLine($"StreamWriterTest: GEN0: {gen0Passes}; GEN1: {gen1Passes}; GEN2: {gen2Passes};");
            }
        }
    }
}
