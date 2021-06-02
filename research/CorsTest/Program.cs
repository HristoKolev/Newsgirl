namespace CorsTest
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newsgirl.Server.Http;

    internal static class Program
    {
        private static readonly HashSet<string> OriginWhiteList = new(new[] {"http://localhost:3000"});

        private static async Task Run(HttpContext context)
        {
            var originHeader = context.Request.Headers["Origin"];
            if (!string.IsNullOrWhiteSpace(originHeader) && !OriginWhiteList.Contains(originHeader))
            {
                context.Response.StatusCode = 403;
                return;
            }

            Console.WriteLine(new string('-', 120));

            Console.WriteLine(context.Request.Method + " " + context.Request.Path);

            foreach ((string key, var value) in context.Request.Headers)
            {
                Console.WriteLine(key + ": " + value);
            }

            Console.WriteLine();
            Console.WriteLine(await context.Request.ReadUtf8());

            context.Response.Headers["Cross-Origin-Resource-Policy"] = "same-origin";
        }

        private static async Task Main()
        {
            await using (var server = new CustomHttpServerImpl())
            {
                await server.Start(Run, new[] {"http://localhost:7300"});

                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
    }
}
