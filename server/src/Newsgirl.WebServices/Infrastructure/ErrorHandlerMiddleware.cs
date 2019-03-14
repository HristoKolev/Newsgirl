namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    using Newtonsoft.Json;

    public class CustomExceptionHandlerMiddleware
    {
        /// <summary>
        ///     This calls the internal ASP.NET mechanism for logging.
        /// </summary>
        private readonly ILogger<CustomExceptionHandlerMiddleware> logger;

        private readonly RequestDelegate next;

        public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            this.logger = logger;
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await this.next(ctx);
            }
            catch (Exception exception)
            {
                await Global.Log.LogError(exception);

                this.logger.LogError(
                    exception, "An error occurred and was caught by the CustomExceptionHandlerMiddleware.");

                var result = ApiResult.FromErrorMessage("An error occurred on the server.");

                string json = JsonConvert.SerializeObject(result, SerializerSettings);

                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = 200;

                await ctx.Response.WriteAsync(json);
            }
        }
    }

    public static class CustomExceptionHandlerMiddlewareExtensions
    {
        public static IApplicationBuilder UseCustomExceptionHandlerMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<CustomExceptionHandlerMiddleware>();
        }
    }
}