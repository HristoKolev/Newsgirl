namespace Newsgirl.Server
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Text;
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
    using Microsoft.IO;
    using Shared;

    /// <summary>
    ///     A wrapper around ASP.NET Core's IHost.
    ///     TODO: What happens when a request is aborted.
    ///     TODO: What happens when a request is in process and we dispose the server.
    /// </summary>
    public class CustomHttpServerImpl : CustomHttpServer
    {
        private readonly CustomHttpServerConfig config;
        private readonly RequestDelegate requestDelegate;

        private bool disposed;
        private IHost host;
        private bool started;

        public CustomHttpServerImpl(CustomHttpServerConfig config, RequestDelegate requestDelegate)
        {
            this.config = config;
            this.requestDelegate = requestDelegate;
        }
        
        public event Action<string[]> Started;
        
        public event Action Stopping;
        
        public event Action Stopped;

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

            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToArray();

            lifetime.ApplicationStarted.Register(() => this.Started?.Invoke(addresses));
            lifetime.ApplicationStopping.Register(() => this.Stopping?.Invoke());
            lifetime.ApplicationStopped.Register(() => this.Stopped?.Invoke());

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

    public class CustomHttpServerConfig
    {
        public string[] Addresses { get; set; }
    }

    public interface CustomHttpServer : IAsyncDisposable
    {
        string FirstAddress { get; }
        
        public event Action<string[]> Started;
        
        public event Action Stopping;
        
        public event Action Stopped;

        Task Start();

        Task Stop();
    }
    
    public static class CustomHttpServerExtensions
    {
        private static readonly RecyclableMemoryStreamManager MemoryStreamManager = new RecyclableMemoryStreamManager();

        /// <summary>
        ///     Writes a string in UTF-8 encoding and closes the stream.
        /// </summary>
        public static async ValueTask WriteUtf8(this HttpResponse response, string str)
        {
            try
            {
                await using (var writer = new StreamWriter(response.Body, EncodingHelper.UTF8))
                {
                    await writer.WriteAsync(str);
                }
            }
            catch (EncoderFallbackException err)
            {
                throw new DetailedLogException("Failed to encode UTF8 response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY"
                };
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write to HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_TO_RESPONSE_BODY"
                };
            }
        }

        /// <summary>
        ///     Reads the request stream to the end and decodes it into a <see cref="string" />.
        ///     Throws on invalid UTF8.
        /// </summary>
        public static async ValueTask<string> ReadUtf8(this HttpRequest request)
        {
            using var requestContent = await request.ReadToEnd();

            string requestBodyString;

            try
            {
                // Decode UTF8.
                requestBodyString = EncodingHelper.UTF8.GetString(requestContent.AsSpan());
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to decode UTF8 from HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_DECODE_UTF8_REQUEST_BODY",
                    Details =
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestContent.AsSpan())}
                    }
                };
            }

            return requestBodyString;
        }

        /// <summary>
        ///     Reads the request stream to the end and returns <see cref="RentedByteArrayHandle" /> with the contents.
        /// </summary>
        public static async ValueTask<RentedByteArrayHandle> ReadToEnd(this HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                var bufferHandle = new RentedByteArrayHandle((int) request.ContentLength.Value);

                try
                {
                    int read;
                    int offset = 0;

                    var buffer = bufferHandle.GetRentedArray();

                    while ((read = await request.Body.ReadAsync(buffer, offset, bufferHandle.Length - offset)) > 0)
                    {
                        offset += read;
                    }
                }
                catch (Exception err)
                {
                    int length = bufferHandle.Length;
                    bufferHandle.Dispose();

                    throw new DetailedLogException("Failed to read the HTTP request body.", err)
                    {
                        Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                        Details =
                        {
                            {"contentLength", length}
                        }
                    };
                }

                return bufferHandle;
            }
            
            var memoryStream = MemoryStreamManager.GetStream();
            
            try
            {
                await request.Body.CopyToAsync(memoryStream);
            }
            catch (Exception err)
            {
                long length = memoryStream.Length;
                // ReSharper disable once MethodHasAsyncOverload
                memoryStream.Dispose();

                throw new DetailedLogException("Failed to read the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                    Details =
                    {
                        {"contentLength", length}
                    }
                };
            }

            return new RentedByteArrayHandle(memoryStream);
        }
    }
}
