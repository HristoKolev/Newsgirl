namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Api;

    using Auth;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    public static class WebServer
    {
        /// <summary>
        /// Runs the web server. Logs to Sentry on failure to start.
        /// </summary>
        public static async Task<int> Run(string[] args)
        {
            try
            {
                new WebHostBuilder()
                    .UseKestrel(opt =>
                    {
                        opt.AddServerHeader = false; 
                        opt.Listen(IPAddress.Any, Global.Settings.WebServerPort);
                    })
                   .UseContentRoot(Global.RootDirectory)
                   .UseStartup<Startup>()
                   .Build()
                   .Run();
            }
            catch (Exception ex)
            {
                await Global.Log.LogError(ex);
                return 1;
            }

            return 0;
        }
    }
    
    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
            app.UseCustomExceptionHandlerMiddleware();
            
            app.Use(async (context, func) =>
            {
                // We aways return JSON data.
                context.Response.Headers["Content-Type"] = "application/json";

                await func();
            });
            
            app.UseAuthMiddleware();
            app.UseApiHandlerProtocolMiddleware();

            app.Run(ctx =>
            {
                ctx.Response.StatusCode = 404;

                return Task.CompletedTask;
            });
        }

        public IServiceProvider ConfigureServices(IServiceCollection services)
        {
            services.AddHttpContextAccessor();

            services.AddLogging(opt =>
            {
                opt.AddConsole();
                opt.AddDebug();
            });

            var container = Global.CreateIoC(services);
            
            return container.GetInstance<IServiceProvider>();
        }
    }
}