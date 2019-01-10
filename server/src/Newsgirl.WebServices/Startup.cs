namespace Newsgirl.WebServices
{
    using Newsgirl.WebServices.Auth;
    using System;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Hosting;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;
    using Newsgirl.WebServices.Infrastructure;
    using StructureMap;

    public class Startup
    {
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole(GetAspCoreLogLevel());

            if (env.IsDevelopment())
            {
                loggerFactory.AddDebug();
            }

            app.Use(async (context, func) =>
            {
                context.Response.Headers["Content-Type"] = "application/json";

                await func();
            });

            app.UseCustomExceptionHandlerMiddleware();
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
            services.AddLogging();

            var container = new Container(config =>
            {
                config.Populate(services);
                config.AddRegistry<MainRegistry>();
            });

            return container.GetInstance<IServiceProvider>();
        }

        private static LogLevel GetAspCoreLogLevel()
        {
            LogLevel logLevel;

            const string DefaultLevel = "Debug";

            if (!Enum.TryParse(Global.AppConfig.AspNetLoggingLevel ?? DefaultLevel, out logLevel))
            {
                logLevel = LogLevel.Debug;
            }

            return logLevel;
        }
    }
}