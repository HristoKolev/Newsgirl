namespace Newsgirl.WebServices
{
    using System;
    using System.Threading.Tasks;

    using Auth;

    using Infrastructure;
    using Infrastructure.Api;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.Extensions.DependencyInjection;
    using Microsoft.Extensions.Logging;

    using StructureMap;

    public class Startup
    {
        public void Configure(IApplicationBuilder app)
        {
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

            services.AddLogging(opt =>
            {
                opt.AddConsole();
                opt.AddDebug();
            });

            var container = new Container(config =>
            {
                config.Populate(services);
                config.AddRegistry<MainRegistry>();
            });

            return container.GetInstance<IServiceProvider>();
        }
    }
}