namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.IO;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public class ApiHandlerProtocolMiddleware
    {
        private readonly RequestDelegate next;

        public ApiHandlerProtocolMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        private static JsonSerializerSettings SerializerSettings { get; } = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };

        private async Task<ApiResult> ProcessRequest(string requestBody, IServiceProvider serviceProvider)
        {
            if (string.IsNullOrWhiteSpace(requestBody))
            {
                return ApiResult.FromErrorMessage("The request body is empty.");
            }

            var jsonRequest = JObject.Parse(requestBody);
            
            string requestType = jsonRequest.GetValue("type", StringComparison.InvariantCultureIgnoreCase).ToString();
            
            if (string.IsNullOrWhiteSpace(requestType))
            {
                return ApiResult.FromErrorMessage("The request type is empty.");
            }

            var handlers = Global.Handlers;

            var handler = handlers.GetHandler(requestType);

            if (handler == null)
            {
                return ApiResult.FromErrorMessage($"No handler found for request type `{requestType}`.");
            }

            string requestPayloadJson = 
                jsonRequest.GetValue("payload", StringComparison.InvariantCultureIgnoreCase).ToString();

            object requestPayload = 
                JsonConvert.DeserializeObject(requestPayloadJson, handler.RequestType, SerializerSettings);

            return await ApiHandlerProtocol.ProcessRequest(requestType, requestPayload, handlers, serviceProvider);
        }
        
        public async Task InvokeAsync(HttpContext context, IServiceProvider serviceProvider)
        {
            if (context.Request.Path != "/api/endpoint")
            {
                await this.next(context);

                return;
            }

            string requestBody;
            
            using (var bodyStream = context.Request.Body)
            using (var streamReader = new StreamReader(bodyStream))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }

            var apiResult = await this.ProcessRequest(requestBody, serviceProvider);
            
            string responseBody = JsonConvert.SerializeObject(apiResult, SerializerSettings);
            
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