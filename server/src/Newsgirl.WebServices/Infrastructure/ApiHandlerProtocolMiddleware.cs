namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    using Newtonsoft.Json;

    public class ApiHandlerProtocolMiddleware
    {
        private readonly RequestDelegate next;

        public ApiHandlerProtocolMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.Request.Path != "/api/endpoint")
            {
                await this.next(context);

                return;
            }

            string body;

            using (var bodyStream = context.Request.Body)
            using (var streamReader = new StreamReader(bodyStream))
            {
                body = await streamReader.ReadToEndAsync();
            }

            var result = await ApiHandlerProtocol.ProcessRequest(
                body,
                Global.Handlers,
                serviceProvider
            );

            string responseBody = JsonConvert.SerializeObject(result, ApiHandlerProtocol.SerializerSettings);

            await context.Response.WriteAsync(responseBody);
        }
    }

    public static class ApiHandlerProtocolMiddlewareExtensions
    {
        public static IApplicationBuilder UseApiHandlerProtocolMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<ApiHandlerProtocolMiddleware>();
        }
    }
}