using System;
using System.Buffers;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Server
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            string address = "http://127.0.0.1:5000";
            
            var config = new HttpServerConfig
            {
                BindAddresses = new []
                {
                    address
                },
            };
            
            var log = new CustomLogger(new CustomLoggerConfig
            {
                DisableSentryIntegration = true,
                DisableConsoleLogging = true,
            });
            
            var server = new AspNetCoreHttpServerImpl(log, config);

            await server.Start(ProcessRequest);
            
            var client = new HttpClient
            {
                BaseAddress = new Uri(address) 
            };

            var data = new string('a', 100 * 1024 * 1024);

            var response = await client.PostAsync("/", new StringContent(data, EncodingHelper.UTF8));

            await server.Stop();
        }

        private static async Task ProcessRequest(HttpContext context)
        {
            int contentLength = (int)context.Request.ContentLength.Value;

            var bufferPool = ArrayPool<byte>.Shared;
            
            byte[] buffer = bufferPool.Rent(contentLength);

            var stream = context.Request.Body;

            try
            {
                try
                {
                    while (true)
                    {
                        int read;

                        int offset = 0;

                        while ((read = await stream.ReadAsync(buffer, offset, contentLength - offset)) > 0)
                        {
                            offset += read;
                        }
                    }
                }
                catch (Exception)
                {
                    bufferPool.Return(buffer);
                    throw;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                throw;
            }
        }
    }

    public class AspNetCoreHttpServerImpl : AspNetCoreHttpServer, IAsyncDisposable
    {
        private readonly ILog log;
        private readonly HttpServerConfig config;
        private IWebHost webHost;
        private RequestDelegate requestDelegate;

        public AspNetCoreHttpServerImpl(ILog log, HttpServerConfig config)
        {
            this.log = log;
            this.config = config;
        }
        
        public Task Start(RequestDelegate onRequest)
        {
            var host = new WebHostBuilder()
                .UseKestrel()
                .ConfigureKestrel(options =>
                {
                    options.AddServerHeader = false;
                    options.AllowSynchronousIO = false;
                })
                .Configure(this.Configure)
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddLogging(x => x.ClearProviders());
                })
                .UseEnvironment("production")
                .CaptureStartupErrors(false)
                .SuppressStatusMessages(true)
                .UseUrls(this.config.BindAddresses)
                .Build();

            this.webHost = host;
            this.requestDelegate = onRequest;
            
            return this.webHost.StartAsync(new CancellationTokenSource(this.config.StartTimeout).Token);
        }

        public Task Stop()
        {
            return this.webHost.StopAsync(new CancellationTokenSource(this.config.StopTimeout).Token);
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

        public async ValueTask DisposeAsync()
        {
            await this.Stop();
            
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

    public interface AspNetCoreHttpServer
    {
        Task Start(RequestDelegate onRequest);
        
        Task Stop();
    }
}
