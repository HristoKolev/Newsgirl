using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Newsgirl.Server
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            var config = new HttpServerConfig
            {
                BindAddresses = new []{"http://127.0.0.1:5000"},
            };

            await using (var server = await AspNetCoreHttpServer.Start(config))
            {
            }
        }
    }

    public class AspNetCoreHttpServer : IAsyncDisposable
    {
        private HttpServerConfig config;
        private IWebHost webHost;

        private AspNetCoreHttpServer()
        {
        }
        
        public static async Task<AspNetCoreHttpServer> Start(HttpServerConfig config)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .ConfigureKestrel(ConfigureKestrel)
                .Configure(Configure)
                .ConfigureServices(ConfigureServices)
                .UseEnvironment("production")
                .CaptureStartupErrors(false)
                .SuppressStatusMessages(true)
                .UseUrls(config.BindAddresses)
                .Build();

            var server = new AspNetCoreHttpServer
            {
                config = config,
                webHost = host
            };

            await server.webHost.StartAsync(new CancellationTokenSource(config.StartTimeout).Token);

            return server;
        }

        private static void ConfigureKestrel(KestrelServerOptions options)
        {
            options.AddServerHeader = false;
        }
        
        private static void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(x => x.ClearProviders());
        }
        
        private static void Configure(IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToList();

            lifetime.ApplicationStarted.Register(() =>
            {
                Console.WriteLine($"HTTP server is UP on {string.Join("; ", addresses)}.");
            });
            
            lifetime.ApplicationStopping.Register(() =>
            {
                Console.WriteLine($"HTTP server is shutting down...");
            });
            
            lifetime.ApplicationStopped.Register(() =>
            {
                Console.WriteLine($"HTTP server is down...");
            });
        }

        public async ValueTask DisposeAsync()
        {
            await this.webHost.StopAsync(new CancellationTokenSource(this.config.StopTimeout).Token);
            
            if (this.webHost is IAsyncDisposable asyncDisposable)
            {
                await asyncDisposable.DisposeAsync();
            }
            else
            {
                throw new ApplicationException($"Host type {this.webHost.GetType().Name} is not IAsyncDisposable.");                
            }
        }
    }

    public class HttpServerConfig
    {
        public HttpServerConfig()
        {
            this.StartTimeout = TimeSpan.FromSeconds(1);
            this.StopTimeout = TimeSpan.FromSeconds(1);
        }
        
        public string[] BindAddresses { get; set; }
        
        public TimeSpan StartTimeout { get; set; }
        
        public TimeSpan StopTimeout { get; set; }
    }

    public interface HttpServer : IAsyncDisposable
    {
        
    }
}
