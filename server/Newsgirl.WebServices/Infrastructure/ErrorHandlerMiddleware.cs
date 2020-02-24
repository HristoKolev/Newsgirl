﻿namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Threading.Tasks;

    using Api;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.Logging;

    /// <summary>
    /// Exception handling middleware. 
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class CustomExceptionHandlerMiddleware
    {
        /// <summary>
        /// This calls the internal ASP.NET mechanism for logging.
        /// </summary>
        private readonly ILogger<CustomExceptionHandlerMiddleware> logger;

        private readonly RequestDelegate next;

        public CustomExceptionHandlerMiddleware(RequestDelegate next, ILogger<CustomExceptionHandlerMiddleware> logger)
        {
            this.logger = logger;
            this.next = next;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task InvokeAsync(HttpContext ctx)
        {
            try
            {
                await this.next(ctx);
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                this.logger.LogError(
                    exception, 
                    $"An error occurred and was caught by the {nameof(CustomExceptionHandlerMiddleware)}."
                );

                var result = ApiResult.FromErrorMessage("An error occurred on the server.");

                ctx.Response.ContentType = "application/json";
                ctx.Response.StatusCode = 200;

                string json = ApiResultJsonHelper.Serialize(result);
                
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