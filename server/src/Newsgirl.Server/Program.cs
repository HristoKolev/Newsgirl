using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Newsgirl.Server
{
    public class Program
    {
        public static void Main(string[] args)
        {
            
        }
    }

    public class HttpServer : IAsyncDisposable
    {
        private readonly IWebHost webHost;
        
        private HttpServer(IWebHost webHost)
        {
            this.webHost = webHost;
        }
        
        public static async Task<HttpServer> Listen()
        {
            var host = new WebHostBuilder()
                .UseKestrel(ConfigureKestrel)
                .Configure(Configure)
                .ConfigureServices(ConfigureServices)
                .UseSetting(WebHostDefaults.EnvironmentKey, "production")
                .UseSetting(WebHostDefaults.CaptureStartupErrorsKey, "false" )
                .UseSetting(WebHostDefaults.SuppressStatusMessagesKey, "true")
                .UseUrls()
                .Build();

            await host.StartAsync();
            
            return new HttpServer(host);
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.AddServerHeader = false;
            options.Listen(IPAddress.Parse("127.0.0.1"), 5000);
        }
        
        private static void ConfigureServices(WebHostBuilderContext webHostBuilderContext, IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(x => x.ClearProviders());
        }
        
        private static void Configure(WebHostBuilderContext webHostBuilderContext, IApplicationBuilder applicationBuilder)
        {
            // lifetime.ApplicationStarted.Register(() => Console.WriteLine("[SYSTEM] Web server is up on http://127.0.0.1:5000. Press Ctrl+C to shut down."));
            // lifetime.ApplicationStopping.Register(() => Console.WriteLine("[SYSTEM] Web server is shutting down..."));
            // lifetime.ApplicationStopped.Register(() => Console.WriteLine("[SYSTEM] Web server is down."));
        }

        public async ValueTask DisposeAsync()
        {
            await this.webHost.StopAsync();
            
            if (this.webHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }

            throw new ApplicationException($"Host type {this.webHost.GetType().Name} is not IAsyncDisposable.");
        }
    }
}
