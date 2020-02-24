﻿namespace Newsgirl.WebServices.Auth
{
    using System;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Infrastructure;
    using Infrastructure.Data;

    using Microsoft.AspNetCore.Builder;
    using Microsoft.AspNetCore.Http;

    /// <summary>
    /// The middleware responsible for authenticating new requests.
    /// </summary>
    // ReSharper disable once ClassNeverInstantiated.Global
    public class AuthMiddleware
    {
        private const string AuthorizationHeaderName = "Authorization";

        private const string SchemeName = "JWT";

        private readonly RequestDelegate next;

        public AuthMiddleware(RequestDelegate next)
        {
            this.next = next;
        }

        /// <summary>
        /// Returns the JWT if found from the `Authorization` header or NULL if not found. 
        /// </summary>
        private static string GetToken(HttpContext context, MainLogger logger)
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

        // ReSharper disable once UnusedMember.Global
        public async Task InvokeAsync(HttpContext context,  JwtService jwtService, AuthService authService, MainLogger logger)
        {
            var requestSession = new RequestSession
            {
                IsAuthenticated = false
            };

            context.SetRequestSession(requestSession);

            string token = GetToken(context, logger);

            if (token == null)
            {
                // Invalid authorization header
                await this.next(context);

                return;
            }

            var publicSession = await jwtService.DecodeSession(token);

            if (publicSession == null)
            {
                // Invalid/Expired token.
                await this.next(context);

                return;
            }

            // The session is decoded and validated.
            var user = await authService.GetUser(publicSession.SessionID);

            if (user == null)
            {
                // No such session in the database.
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

    /// <summary>
    /// Extension methods that hide the fact the session is stored in the HttpContext.Items.
    /// </summary>
    public static class RequestExtensions
    {
        private const string RequestSessionKey = "__request_session__";

        public static RequestSession GetRequestSession(this HttpContext ctx)
        {
            return (RequestSession) ctx.Items[RequestSessionKey];
        }

        public static void SetRequestSession(this HttpContext ctx, RequestSession requestSession)
        {
            ctx.Items[RequestSessionKey] = requestSession;
        }
    }
    
    public class RequestSession
    {
        public UserBM CurrentUser { get; set; }

        public bool IsAuthenticated { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class JwtService : JwtService<PublicUserModel>
    {
        public JwtService(MainLogger logger, ObjectPool<X509Certificate2> certPool)
            : base(logger, certPool)
        {
        }
    }

    /// <summary>
    /// The model stored in the JWT.
    /// </summary>
    public class PublicUserModel
    {
        public int SessionID { get; set; }
    }
}