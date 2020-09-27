namespace Newsgirl.Server.Http
{
    using System;
    using System.Buffers;
    using System.IO;
    using System.Linq;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Hosting.Server;
    using Microsoft.AspNetCore.Hosting.Server.Features;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using Microsoft.Toolkit.HighPerformance.Buffers;
    using Shared;

    /// <summary>
    /// A wrapper around ASP.NET Core's IHost.
    /// </summary>
    public class CustomHttpServerImpl : CustomHttpServer
    {
        private bool started;
        private bool disposed;

        private IHost host;
        private string[] boundAddresses;

        /// <summary>
        /// Fires when the server starts with the bound addresses as an argument.
        /// </summary>
        public event Action<string[]> Started;

        /// <summary>
        /// Fires when shutdown is triggered.
        /// </summary>
        public event Action Stopping;

        /// <summary>
        /// Fires when the server is properly shut down.
        /// </summary>
        public event Action Stopped;

        public async Task Start(RequestDelegate requestDelegate, string[] listenOnAddresses)
        {
            this.ThrowIfDisposed();
            this.ThrowIfStarted();

            this.host = new HostBuilder().ConfigureWebHost(builder =>
                {
                    builder.UseKestrel()
                        .ConfigureKestrel(options =>
                        {
                            options.AddServerHeader = false;
                            options.AllowSynchronousIO = false;
                        })
                        .Configure(app =>
                        {
                            var lifetime = app.ApplicationServices.GetService<IHostApplicationLifetime>();

                            var addresses = app.ServerFeatures.Get<IServerAddressesFeature>().Addresses.ToArray();

                            lifetime!.ApplicationStarted.Register(() => this.Started?.Invoke(addresses));
                            lifetime.ApplicationStopping.Register(() => this.Stopping?.Invoke());
                            lifetime.ApplicationStopped.Register(() => this.Stopped?.Invoke());

                            app.Use(_ => requestDelegate);
                        })
                        .CaptureStartupErrors(false)
                        .SuppressStatusMessages(true)
                        .UseUrls(listenOnAddresses);
                })
                .ConfigureServices(serviceCollection =>
                {
                    serviceCollection.AddLogging(x => x.ClearProviders());
                    serviceCollection.AddSingleton<IHostLifetime, EmptyLifetime>();
                })
                .UseEnvironment("production")
                .Build();

            await this.host.StartAsync();

            var server = this.host.Services.GetService<IServer>();
            var addressesFeature = server!.Features.Get<IServerAddressesFeature>();

            this.boundAddresses = addressesFeature.Addresses.ToArray();

            this.started = true;
        }

        public async Task Stop()
        {
            this.ThrowIfDisposed();
            this.ThrowIfStopped();

            var lifetime = this.host.Services.GetService<IHostApplicationLifetime>();

            var stoppingFired = new TaskCompletionSource<object>(TaskCreationOptions.RunContinuationsAsynchronously);
            lifetime!.ApplicationStopping.Register(() => stoppingFired.TrySetResult(null));
            lifetime.StopApplication();

            await stoppingFired.Task;

            await this.host.StopAsync();

            // ReSharper disable once SuspiciousTypeConversion.Global
            var asyncDisposable = (IAsyncDisposable) this.host;
            await asyncDisposable.DisposeAsync();
            this.host = null;

            this.boundAddresses = null;

            this.started = false;
        }

        public string[] BoundAddresses
        {
            get
            {
                this.ThrowIfDisposed();
                this.ThrowIfStopped();

                return this.boundAddresses;
            }
        }

        public async ValueTask DisposeAsync()
        {
            if (this.disposed)
            {
                return;
            }

            if (this.started)
            {
                await this.Stop();
            }

            this.disposed = true;
        }

        private void ThrowIfStarted()
        {
            if (this.started)
            {
                throw new NotSupportedException("The server must be stopped to perform this operation.");
            }
        }

        private void ThrowIfStopped()
        {
            if (!this.started)
            {
                throw new NotSupportedException("The server must be started to perform this operation.");
            }
        }

        private void ThrowIfDisposed()
        {
            if (this.disposed)
            {
                throw new ObjectDisposedException("AspNetCoreHttpServerImpl");
            }
        }

        private class EmptyLifetime : IHostLifetime
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

    public interface CustomHttpServer : IAsyncDisposable
    {
        string[] BoundAddresses { get; }

        public event Action<string[]> Started;

        public event Action Stopping;

        public event Action Stopped;

        Task Start(RequestDelegate requestDelegate, string[] addresses);

        Task Stop();
    }

    public static class HttpContextExtensions
    {
        /// <summary>
        /// Writes a string in UTF-8 encoding and closes the stream.
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
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY",
                };
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write to HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_TO_RESPONSE_BODY",
                };
            }
        }

        /// <summary>
        /// Reads the request stream to the end and decodes it into a <see cref="string" />.
        /// Throws on invalid UTF8.
        /// </summary>
        public static async ValueTask<string> ReadUtf8(this HttpRequest request)
        {
            using var requestContent = await request.ReadToEnd();

            string requestBodyString;

            try
            {
                // Decode UTF8.
                requestBodyString = EncodingHelper.UTF8.GetString(requestContent.Memory.Span);
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to decode UTF8 from HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_DECODE_UTF8_REQUEST_BODY",
                    Details =
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestContent.Memory.Span)},
                    },
                };
            }

            return requestBodyString;
        }

        /// <summary>
        /// Reads the request stream to the end and returns <see cref="IMemoryOwner{T}" /> with the contents.
        /// </summary>
        public static async ValueTask<IMemoryOwner<byte>> ReadToEnd(this HttpRequest request)
        {
            if (request.ContentLength.HasValue)
            {
                var memoryOwner = MemoryOwner<byte>.Allocate((int) request.ContentLength.Value);

                try
                {
                    var memory = memoryOwner.Memory;
                    int read;
                    int offset = 0;

                    while ((read = await request.Body.ReadAsync(memory.Slice(offset, memory.Length - offset))) > 0)
                    {
                        offset += read;
                    }
                }
                catch (Exception err)
                {
                    memoryOwner.Dispose();

                    throw new DetailedLogException("Failed to read the HTTP request body.", err)
                    {
                        Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                        Details =
                        {
                            {"contentLength", request.ContentLength.Value},
                        },
                    };
                }

                return memoryOwner;
            }

            try
            {
                return await request.Body.ReadUnknownSizeStream();
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to read the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                };
            }
        }
    }
}
