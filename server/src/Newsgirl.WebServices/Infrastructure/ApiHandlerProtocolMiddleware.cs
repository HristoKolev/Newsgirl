using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;

namespace Newsgirl.WebServices.Infrastructure
{
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

            var message = JsonConvert.DeserializeObject<ApiRequest>(body, ApiHandlerProtocol.SerializerSettings);

            var result = await ApiHandlerProtocol.ProcessRequest(
                message,
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