namespace Newsgirl.Server
{
    using System;
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
    using Shared.Infrastructure;

    /// <summary>
    ///     A wrapper around ASP.NET Core's IHost.
    /// </summary>
    public class HttpServerImpl : HttpServer, IAsyncDisposable
    {
        private readonly ILog log;
        private readonly RequestDelegate requestDelegate;

        private bool disposed;
        private IHost host;

        public HttpServerImpl(ILog log, HttpServerConfig config, RequestDelegate requestDelegate)
        {
            this.log = log;
            this.requestDelegate = requestDelegate;
            this.host = new HostBuilder()
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
                        .UseUrls(config.Addresses);
                })
                .ConfigureServices(serviceCollection => { serviceCollection.AddLogging(x => x.ClearProviders()); })
                .UseEnvironment("production")
                .Build();
        }

        public Task Start()
        {
            this.ThrowIfDisposed();

            return this.host.StartAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        }

        public Task Stop()
        {
            this.ThrowIfDisposed();

            return this.host.StopAsync(new CancellationTokenSource(TimeSpan.FromSeconds(1)).Token);
        }

        public string FirstAddress
        {
            get
            {
                this.ThrowIfDisposed();

                var server = this.host.Services.GetService<IServer>();
                var addressesFeature = server.Features.Get<IServerAddressesFeature>();
                return addressesFeature.Addresses.FirstOrDefault();
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            await this.Stop();

            // ReSharper disable once SuspiciousTypeConversion.Global
            var asyncDisposable = (IAsyncDisposable) this.host;
            await asyncDisposable.DisposeAsync();
            this.host = null;

            this.disposed = true;
        }

        private void Configure(IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToList();

            lifetime.ApplicationStarted.Register(() =>
            {
                this.log.Log($"HTTP server is UP on {string.Join("; ", addresses)} ...");
            });

            lifetime.ApplicationStopping.Register(() => { this.log.Log("HTTP server is shutting down ..."); });

            lifetime.ApplicationStopped.Register(() => { this.log.Log("HTTP server is down ..."); });

            app.Use(_ => this.requestDelegate);
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("AspNetCoreHttpServerImpl");
            }
        }
    }

    public class HttpServerConfig
    {
        public string[] Addresses { get; set; }
    }

    public interface HttpServer
    {
        string FirstAddress { get; }
        
        Task Start();

        Task Stop();
    }
}
