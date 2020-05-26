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
    using Shared.Logging;

    /// <summary>
    ///     A wrapper around ASP.NET Core's IHost.
    ///     TODO: What happens when a request is aborted.
    ///     TODO: What happens when a request is in process and we dispose the server.
    /// </summary>
    public class HttpServerImpl : HttpServer
    {
        private readonly ILog log;
        private readonly HttpServerConfig config;
        private readonly RequestDelegate requestDelegate;

        private bool disposed;
        private IHost host;
        private bool started;

        public HttpServerImpl(ILog log, HttpServerConfig config, RequestDelegate requestDelegate)
        {
            this.log = log;
            this.config = config;
            this.requestDelegate = requestDelegate;
        }

        public async Task Start()
        {
            this.ThrowIfDisposed();
            this.ThrowIfStarted();
            
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
                        .UseUrls(this.config.Addresses);
                })
                .ConfigureServices(this.ConfigureServices)
                .UseEnvironment("production")
                .Build();

            await this.host.StartAsync();

            this.started = true;
        }

        private void ConfigureServices(IServiceCollection serviceCollection)
        {
            serviceCollection.AddLogging(x => x.ClearProviders());
            serviceCollection.AddSingleton<IHostLifetime, EmptyLifetime>();
        }

        public async Task Stop()
        {
            this.ThrowIfDisposed();
            this.ThrowIfStopped();
            
            var lifetime = this.host.Services.GetService<IHostApplicationLifetime>();
            
            var stoppingFired = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            lifetime.ApplicationStopping.Register(() => stoppingFired.TrySetResult(null));
            lifetime.StopApplication();

            await stoppingFired.Task;
            
            await this.host.StopAsync();

            // ReSharper disable once SuspiciousTypeConversion.Global
            var asyncDisposable = (IAsyncDisposable) this.host;
            await asyncDisposable.DisposeAsync();
            this.host = null;

            this.started = false;
        }

        public string FirstAddress
        {
            get
            {
                this.ThrowIfDisposed();
                this.ThrowIfStopped();

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

            this.disposed = true;
        }

        private void Configure(IApplicationBuilder app)
        {
            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToList();

            lifetime.ApplicationStarted.Register(() =>
            {
                this.log.General(() => new LogData($"HTTP server is UP on {string.Join("; ", addresses)} ..."));
            });

            lifetime.ApplicationStopping.Register(() => { this.log.General(() => new LogData("HTTP server is shutting down ...")); });

            lifetime.ApplicationStopped.Register(() => { this.log.General(() => new LogData("HTTP server is down ...")); });

            app.Use(_ => this.requestDelegate);
        }

        private void ThrowIfStarted()
        {
            if (this.started)
            {
                throw new NotSupportedException("The server must be started to perform this operation.");
            }
        }
        
        private void ThrowIfStopped()
        {
            if (!this.started)
            {
                throw new NotSupportedException("The server must be stopped to perform this operation.");
            }
        }
        
        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("AspNetCoreHttpServerImpl");
            }
        }

        public class EmptyLifetime : IHostLifetime
        {
            public Task StopAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }

            public Task WaitForStartAsync(CancellationToken cancellationToken)
            {
                return Task.CompletedTask;
            }
        }
    }

    public class HttpServerConfig
    {
        public string[] Addresses { get; set; }
    }

    public interface HttpServer : IAsyncDisposable
    {
        string FirstAddress { get; }
        
        Task Start();

        Task Stop();
    }
}
