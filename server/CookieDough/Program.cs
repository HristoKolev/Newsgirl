namespace CookieDough
{
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Newsgirl.Server.Http;

    public class Program
    {
        public static async Task Main(string[] args)
        {
            int num = 0;

            async Task Handler(HttpContext context)
            {
                context.Response.Cookies.Append("cats", num++.ToString());
                context.Response.Cookies.Append("cats1", num++.ToString(), new CookieOptions());
                await context.Response.WriteUtf8("test123");
            }

            await using (var server = new CustomHttpServerImpl())
            {
                await server.Start(Handler, new[] {"http://127.0.0.1:6200"});
                Console.WriteLine("Started...");

                await Task.Delay(1000 * 60 * 10);

                await server.Stop();
                Console.WriteLine("Stopped...");
            }
        }
    }
}
