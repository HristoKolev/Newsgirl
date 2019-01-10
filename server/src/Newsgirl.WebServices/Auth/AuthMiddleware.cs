namespace Newsgirl.WebServices.Auth
{
    using System;
    using System.Threading.Tasks;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    using Newsgirl.WebServices.Infrastructure;
    using Newsgirl.WebServices.Infrastructure.Data;

    public class AuthMiddleware
    {
        private const string AuthorizationHeaderName = "Authorization";

        private const string SchemeName = "JWT";

        private readonly RequestDelegate next;

        public AuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        public string GetToken(HttpContext context, MainLogger logger)
        {
            try
            {
                string headerValue = context.Request.Headers[AuthorizationHeaderName];

                if (string.IsNullOrWhiteSpace(headerValue))
                {
                    // Invalid authorization header
                    return null;
                }

                var parts = headerValue.Split(' ', StringSplitOptions.RemoveEmptyEntries);

                if (parts.Length != 2)
                {
                    // Invalid authorization header
                    return null;
                }

                string scheme = parts[0];

                if (scheme != SchemeName)
                {
                    // Unsupported scheme.
                    return null;
                }

                string token = parts[1];

                return token;
            }
            catch (Exception exception)
            {
                logger.LogError(exception);
                return null;
            }
        }

        public async Task InvokeAsync(HttpContext context, JwtService jwtService, AuthService authService, MainLogger logger)
        {
            var requestSession = new RequestSession
            {
                IsAuthenticated = false,
            };

            context.SetRequestSession(requestSession);

            string token = this.GetToken(context, logger);

            if (token == null)
            {
                // Invalid authorization header
                await this.next(context);
                return;
            }

            var publicSession = jwtService.DecodeSession(token);

            if (publicSession == null)
            {
                // Invalid/Expired token.
                await this.next(context);
                return;
            }

            var user = await authService.GetUser(publicSession.SessionID);

            if (user == null)
            {
                // No such session.
                await this.next(context);
                return;
            }

            requestSession.IsAuthenticated = true;
            requestSession.CurrentUser = user;

            await this.next(context);
        }
    }

    public static class AuthMiddlewareExtensions
    {
        public static IApplicationBuilder UseAuthMiddleware(this IApplicationBuilder builder)
        {
            return builder.UseMiddleware<AuthMiddleware>();
        }
    }

    public static class RequestExtensions
    {
        private const string RequestClientKey = "__request_client__";

        public static RequestSession GetRequestSession(this HttpContext ctx)
        {
            return (RequestSession)ctx.Items[RequestClientKey];
        }

        public static void SetRequestSession(this HttpContext ctx, RequestSession requestSession)
        {
            ctx.Items[RequestClientKey] = requestSession;
        }
    }

    public class RequestSession
    {
        public UserBM CurrentUser { get; set; }

        public bool IsAuthenticated { get; set; }
    }

    public class JwtService : JwtService<PublicUserModel>
    {
        public JwtService(MainLogger logger)
            : base(logger)
        {
        }
    }

    public class PublicUserModel
    {
        public int SessionID { get; set; }
    }
}