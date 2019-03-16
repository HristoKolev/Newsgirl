namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System.IO;
    using System.Threading.Tasks;

    using Auth;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    public class ApiHandlerProtocolMiddleware
    {
        private readonly RequestDelegate next;

        public ApiHandlerProtocolMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public async Task InvokeAsync(HttpContext context, TypeResolver resolver)
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
            
            var result = ParseRequest(context, requestBody);

            ApiResult apiResult;
            
            if (!result.IsSuccess)
            {
                apiResult = ApiResult.FromErrorMessages(result.ErrorMessages);
            }
            else
            {
                apiResult = await ApiHandlerProtocol.ProcessRequest(result.Value, Global.Handlers, resolver);
            }

            string responseBody = ApiResultJsonHelper.Serialize(apiResult);
            
            await context.Response.WriteAsync(responseBody);
        }

        private static Result<ApiRequest> ParseRequest(HttpContext context, string requestBody)
        {
            var result = ApiResultJsonHelper.TryParse(requestBody);

            if (!result.IsSuccess)
            {
                return result;
            }

            var session = context.GetRequestSession();

            var apiRequest = result.Value;

            var handler = Global.Handlers.GetHandler(apiRequest.Type);
                
            if (handler.RequireAuthentication && !session.IsAuthenticated)
            {
                return Result.FromErrorMessage<ApiRequest>($"Access denied for request type `{apiRequest.Type}`.");
            }

            return result;
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