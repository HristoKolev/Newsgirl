namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System.IO;
    using System.Threading.Tasks;

    using Auth;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ApiHandlerProtocolMiddleware
    {
        private readonly RequestDelegate next;

        public ApiHandlerProtocolMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        // ReSharper disable once UnusedMember.Global
        public async Task InvokeAsync(HttpContext context, TypeResolver resolver)
        {
            // If this is not the specified path - pass it to the next guy.
            if (context.Request.Path != "/api/endpoint")
            {
                await this.next(context);
                return;
            }

            // Read the request body.
            string requestBody;
            
            using (var bodyStream = context.Request.Body)
            using (var streamReader = new StreamReader(bodyStream))
            {
                requestBody = await streamReader.ReadToEndAsync();
            }
            
            // Parse the request.
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

            // Return the response.
            string responseBody = ApiResultJsonHelper.Serialize(apiResult);
            await context.Response.WriteAsync(responseBody);
        }

        /// <summary>
        /// Parses an ApiRequest from the HTTP request stream.
        /// It does integrity validation and access rights validation.
        /// It doesn't do any request payload validation.  
        /// </summary>
        private static Result<ApiRequest> ParseRequest(HttpContext context, string requestBody)
        {
            // It does `ApiRequest` integrity validation.
            var result = ApiResultJsonHelper.TryParse(requestBody);

            if (!result.IsSuccess)
            {
                return result;
            }

            // Access rights validation.
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