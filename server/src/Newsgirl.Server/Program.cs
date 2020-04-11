using System;
using System.Buffers;
using System.Linq;
using System.Net.Http;
using System.Text;
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

            var random = new Random(123);
            
            string data = string.Join("", Enumerable.Range(0, 100 * 1024).Select(i => random.Next(i).ToString()));

            var response = await client.PostAsync("/", new StringContent(data, EncodingHelper.UTF8));

            byte[] responseBytes = await response.Content.ReadAsByteArrayAsync();
            string responseString = EncodingHelper.UTF8.GetString(responseBytes);

            Console.WriteLine(data == responseString);

            await server.Stop();
        }

        private static async Task ProcessRequest(HttpContext context)
        {
            // Read request bytes.
            int contentLength = (int)context.Request.ContentLength.Value;
            var bufferPool = ArrayPool<byte>.Shared;
            var requestStream = context.Request.Body;
            
            byte[] requestBuffer = bufferPool.Rent(contentLength);

            try
            {
                while (await requestStream.ReadAsync(requestBuffer) > 0) {}
            }
            catch (Exception err)
            {
                bufferPool.Return(requestBuffer);
                
                throw new DetailedLogException("An error occurred while reading the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                    Details =
                    {
                        {"contentLength", contentLength},
                    }
                };
            }

            // Decode UTF8.
            static string DecodeUtf8(byte[] bytes, int length)
            {
                return EncodingHelper.UTF8.GetString(new ReadOnlySpan<byte>(bytes, 0, length)); 
            }
            
            string requestBodyString;

            try
            {
                requestBodyString = DecodeUtf8(requestBuffer, contentLength);
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to decode UTF8 from HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_DECODE_UTF8_REQUEST_BODY",
                    Details =
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestBuffer, 0, contentLength)}
                    }
                };
            }
            finally
            {
                bufferPool.Return(requestBuffer);
            }

            // Process request data.
            string responseBodyString = requestBodyString;
            
            byte[] responseBuffer = bufferPool.Rent(EncodingHelper.UTF8.GetMaxByteCount(responseBodyString.Length));

            // Encode UTF8.
            int utf8EncodedByteLength;

            try
            {
                var encoder = EncodingHelper.UTF8.GetEncoder();
                utf8EncodedByteLength = encoder.GetBytes(responseBodyString, responseBuffer, true);
            }
            catch (Exception err)
            {
                bufferPool.Return(responseBuffer);
                
                throw new DetailedLogException("Failed to encode UTF8 response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_ENCODE_UTF8_RESPONSE_BODY",
                };
            }

            // Write to response.
            try
            {
                var responseStream = context.Response.Body;
                
                context.Response.StatusCode = 200;
                await responseStream.WriteAsync(responseBuffer, 0, utf8EncodedByteLength);
                await responseStream.FlushAsync();
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to write HTTP response body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_WRITE_RESPONSE_BODY",
                };
            }
            finally
            {
                bufferPool.Return(responseBuffer);
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
