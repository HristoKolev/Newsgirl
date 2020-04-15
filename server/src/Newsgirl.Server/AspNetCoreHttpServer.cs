using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Server
{
    public class AspNetCoreHttpServerImpl : AspNetCoreHttpServer, IAsyncDisposable
    {
        private readonly ILog log;
        private readonly HttpServerConfig config;
        private IHost host;
        private RequestDelegate requestDelegate;

        public AspNetCoreHttpServerImpl(ILog log, HttpServerConfig config)
        {
            this.log = log;
            this.config = config;
        }
        
        public Task Start(RequestDelegate onRequest)
        {
            var host = new HostBuilder()
                .ConfigureWebHost(builder =>
                {
                    builder.UseKestrel()
                        .ConfigureKestrel(options =>
                        {
                            options.AddServerHeader = false;
                            options.AllowSynchronousIO = false;
                        })
                        .Configure(this.Configure)
                        .CaptureStartupErrors(false)
                        .SuppressStatusMessages(true)
                        .UseUrls(this.config.BindAddresses);
                })
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddLogging(x => x.ClearProviders());
                })
                .UseEnvironment("production")
                .Build();

            this.host = host;
            this.requestDelegate = onRequest;

            return this.host.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        }

        public Task Stop()
        {
            return this.host.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        }

        private void Configure(IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToList();

            lifetime.ApplicationStarted.Register(() =>
            {
                this.log.Log($"HTTP server is UP on {string.Join("; ", addresses)} ...");
            });
            
            lifetime.ApplicationStopping.Register(() =>
            {
                this.log.Log("HTTP server is shutting down ...");
            });
            
            lifetime.ApplicationStopped.Register(() =>
            {
                this.log.Log("HTTP server is down ...");
            });

            app.Use(_ => this.requestDelegate);
        }

        public ICollection<string> GetAddresses()
        {
            var server = this.host.Services.GetService<IServer>();
            var addressesFeature = server.Features.Get<IServerAddressesFeature>();
            return addressesFeature.Addresses;
        }

        public async ValueTask DisposeAsync()
        {
            await this.Stop();

            var asyncDisposable = (IAsyncDisposable)this.host;
            await asyncDisposable.DisposeAsync();
        }
    }
    
    public class HttpServerConfig
    {
        public string[] BindAddresses { get; set; }
    }

    public interface AspNetCoreHttpServer
    {
        Task Start(RequestDelegate onRequest);
        
        Task Stop();
    }
}