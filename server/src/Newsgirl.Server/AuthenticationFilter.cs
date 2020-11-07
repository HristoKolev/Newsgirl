namespace Newsgirl.Server
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Primitives;
    using Shared;

    public class AuthenticationFilter
    {
        private const string COOKIE_HEADER = "Cookie";
        private const string CSRF_TOKEN_HEADER = "Csrf-Token";

        private readonly JwtService jwtService;
        private readonly AuthService authService;
        private readonly DateTimeService dateTimeService;

        public AuthenticationFilter(JwtService jwtService, AuthService authService, DateTimeService dateTimeService)
        {
            this.jwtService = jwtService;
            this.authService = authService;
            this.dateTimeService = dateTimeService;
        }

        private static string GetHeaderValue(IDictionary<string, StringValues> headers, string key)
        {
            if (!headers.TryGetValue(key, out var headerValue))
            {
                return null;
            }

            string stringHeaderValue = headerValue;

            if (string.IsNullOrWhiteSpace(stringHeaderValue))
            {
                return null;
            }

            return stringHeaderValue;
        }

        private static Dictionary<string, string> GetCookies(IDictionary<string, StringValues> headers)
        {
            if (headers == null)
            {
                return null;
            }

            string cookieHeader = GetHeaderValue(headers, COOKIE_HEADER);

            if (cookieHeader == null)
            {
                return null;
            }

            var cookieHeaderParts = cookieHeader.Split(';', StringSplitOptions.RemoveEmptyEntries);

            if (cookieHeaderParts.Length == 0)
            {
                return null;
            }

            var cookieValues = new Dictionary<string, string>();

            for (int i = 0; i < cookieHeaderParts.Length; i++)
            {
                string[] kvp = cookieHeaderParts[i].Split('=', StringSplitOptions.RemoveEmptyEntries);

                if (kvp.Length == 2)
                {
                    string key = kvp[0];
                    string value = kvp[1];

                    if (!string.IsNullOrWhiteSpace(key) && !string.IsNullOrWhiteSpace(value))
                    {
                        cookieValues.Add(key.Trim(), value.Trim());
                    }
                }
            }

            return cookieValues;
        }

        public async Task<AuthResult> Authenticate(IDictionary<string, StringValues> headers)
        {
            var cookies = GetCookies(headers);

            if (cookies == null)
            {
                return AuthResult.Anonymous; // no cookies at all
            }

            string jwt = cookies.GetValueOrDefault("jwt");

            if (string.IsNullOrWhiteSpace(jwt))
            {
                return AuthResult.Anonymous; // no jwt cookie
            }

            var jwtPayload = this.jwtService.DecodeSession<JwtPayload>(jwt);

            if (jwtPayload == null)
            {
                return AuthResult.Anonymous; // invalid jwt
            }

            var userSession = await this.authService.GetSession(jwtPayload.SessionID);

            if (userSession == null)
            {
                return AuthResult.Anonymous; // non existent session
            }

            if (userSession.ExpirationDate.HasValue && this.dateTimeService.EventTime() > userSession.ExpirationDate.Value)
            {
                return AuthResult.Anonymous; // expired session
            }

            string csrfToken = GetHeaderValue(headers, CSRF_TOKEN_HEADER);

            var authResult = new AuthResult
            {
                SessionID = userSession.SessionID,
                LoginID = userSession.LoginID,
                ValidCsrfToken = userSession.CsrfToken == csrfToken,
            };

            return authResult;
        }
    }

    [DebuggerDisplay("SessionID = {" + nameof(SessionID) + "}, ValidCsrfToken = {" + nameof(ValidCsrfToken) + "}")]
    public class AuthResult
    {
        public static readonly AuthResult Anonymous = new AuthResult();

        public int SessionID { get; set; }

        public int LoginID { get; set; }

        public bool ValidCsrfToken { get; set; }
    }
}
