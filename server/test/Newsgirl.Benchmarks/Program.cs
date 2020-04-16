using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using BenchmarkDotNet.Running;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Newsgirl.Benchmarks
{
    public class Program
    {
        private static readonly Dictionary<string, Func<Task>> BenchmarkTable = new Dictionary<string, Func<Task>>
        {
            {"benchmark.net", RunBenchmarkNet},
            {"aspnet-server", AspNetServer.Run},
        };

        private static async Task Main(string[] args)
        {
            string benchmarkName = args.FirstOrDefault();

            if (benchmarkName == null)
            {
                Console.WriteLine("Please, pass benchmark name as a first parameter.");
                return;
            }
            
            Func<Task> benchmarkFunction;

            if (!BenchmarkTable.TryGetValue(benchmarkName.ToLowerInvariant(), out benchmarkFunction))
            {
                Console.WriteLine($"No benchmark found for name: `{benchmarkName}`");
                return;
            }

            Console.WriteLine($"Running benchmark: {benchmarkName}");
            
            await benchmarkFunction();
        }

        private static Task RunBenchmarkNet()
        {
            var args = Environment.GetCommandLineArgs().Skip(2).ToArray();
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
            return Task.CompletedTask;
        }
    }
}
