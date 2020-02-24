namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using Api;

    using Auth;

    using Autofac;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    [CliCommand("web-server", IsDefault = true)]
    // ReSharper disable once UnusedMember.Global
    public class WebServerCommand : ICliCommand
    {
        /// <summary>
        /// Runs the web server. Logs to Sentry on failure to start.
        /// </summary>
        public async Task<int> Run(string[] args)
        {
            try
            {
                new WebHostBuilder()
                    .UseKestrel(opt =>
                    {
                        opt.AddServerHeader = false; 
                        opt.Listen(IPAddress.Any, Global.AppConfig.Port);
                    })
                   .UseContentRoot(Global.RootDirectory)
                   .UseStartup<Startup>()
                   .Build()
                   .Run();
            }
            catch (Exception ex)
            {
                await MainLogger.Instance.LogError(ex);
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
            
            return container.Resolve<IServiceProvider>();
        }
    }
}