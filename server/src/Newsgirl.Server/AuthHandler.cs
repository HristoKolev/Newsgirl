namespace Newsgirl.Server
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using LinqToDB;
    using Shared;
    using Shared.Postgres;

    public class AuthHandler
    {
        private readonly IDbService db;
        private readonly PasswordService passwordService;
        private readonly AuthService authService;
        private readonly JwtService jwtService;
        private readonly DateTimeService dateTimeService;

        public AuthHandler(
            IDbService db,
            PasswordService passwordService,
            AuthService authService,
            JwtService jwtService,
            DateTimeService dateTimeService)
        {
            this.db = db;
            this.passwordService = passwordService;
            this.authService = authService;
            this.jwtService = jwtService;
            this.dateTimeService = dateTimeService;
        }

        [RpcBind(typeof(RegisterRequest), typeof(RegisterResponse))]
        public async Task<RpcResult<RegisterResponse>> Register(RegisterRequest req)
        {
            req.Email = req.Email.Trim().ToLower();
            req.Password = req.Password.Trim();

            var existingLogin = await this.authService.FindLogin(req.Email);

            if (existingLogin != null)
            {
                return "There already is an account with that email address.";
            }

            await using (var tx = await this.db.BeginTransaction())
            {
                var (profile, login) = await this.authService.CreateProfile(req.Email, req.Password);

                var session = await this.authService.CreateSession(login.LoginID, false);

                var result = new RpcResult<RegisterResponse>
                {
                    Payload = new RegisterResponse
                    {
                        CsrfToken = session.CsrfToken,
                        EmailAddress = profile.EmailAddress,
                        UserProfileID = profile.UserProfileID,
                    },
                    Headers =
                    {
                        {"Set-Cookie", this.FormatCookie(session)},
                    },
                };

                await tx.CommitAsync();

                return result;
            }
        }

        [RpcBind(typeof(LoginRequest), typeof(LoginResponse))]
        public async Task<RpcResult<LoginResponse>> Login(LoginRequest req)
        {
            req.Username = req.Username.Trim().ToLower();
            req.Password = req.Password.Trim();

            var login = await this.authService.FindLogin(req.Username);

            if (login == null)
            {
                return "Wrong username or password.";
            }

            if (!login.Enabled)
            {
                return "Wrong username or password.";
            }

            if (!this.passwordService.VerifyPassword(req.Password, login.PasswordHash))
            {
                return "Wrong username or password.";
            }

            var profile = await this.db.Poco.UserProfiles.FirstOrDefaultAsync(x => x.UserProfileID == login.UserProfileID);

            await using (var tx = await this.db.BeginTransaction())
            {
                var session = await this.authService.CreateSession(login.LoginID, req.RememberMe);

                var result = new RpcResult<LoginResponse>
                {
                    Payload = new LoginResponse
                    {
                        CsrfToken = session.CsrfToken,
                        EmailAddress = profile.EmailAddress,
                        UserProfileID = profile.UserProfileID,
                    },
                    Headers =
                    {
                        {"Set-Cookie", this.FormatCookie(session)},
                    },
                };

                await tx.CommitAsync();

                return result;
            }
        }

        [RpcBind(typeof(ProfileInfoRequest), typeof(ProfileInfoResponse))]
#pragma warning disable 1998
        public async Task<ProfileInfoResponse> ProfileInfo(ProfileInfoRequest req, AuthResult authResult)
#pragma warning restore 1998
        {
            return new ProfileInfoResponse();
        }

        private string FormatCookie(UserSessionPoco session)
        {
            var payload = new JwtPayload {SessionID = session.SessionID};
            string token = this.jwtService.EncodeSession(payload);
            DateTime expirationDate = session.ExpirationDate ?? this.dateTimeService.EventTime().AddYears(1000);
            string cookie = $"token={token}; Expires={expirationDate:R}; Secure; HttpOnly";

            return cookie;
        }
    }

    public class AuthenticationMiddleware : RpcMiddleware
    {
        private static Dictionary<string, string> GetCookies(IReadOnlyDictionary<string, string> headers)
        {
            if (headers == null)
            {
                return null;
            }

            string cookieHeader = headers.GetValueOrDefault("Cookie");

            if (string.IsNullOrWhiteSpace(cookieHeader))
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

        private static async Task<AuthResult> Authenticate(RpcRequestMessage requestMessage, JwtService jwtService, AuthService authService)
        {
            var cookies = GetCookies(requestMessage.Headers);

            string token = cookies.GetValueOrDefault("token");

            if (string.IsNullOrWhiteSpace(token))
            {
                return AuthResult.Anonymous;
            }

            var tokenPayload = jwtService.DecodeSession<JwtPayload>(token);

            if (tokenPayload == null)
            {
                return AuthResult.Anonymous;
            }

            string csrfToken = requestMessage.Headers.GetValueOrDefault("Csrf-Token");

            if (string.IsNullOrWhiteSpace(csrfToken))
            {
                return AuthResult.Anonymous;
            }
            
            var userSession = await authService.GetSession(tokenPayload.SessionID);
            
            
            
            
        }

        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            var jwtService = instanceProvider.Get<JwtService>();
            var authService = instanceProvider.Get<AuthService>();

            var authResult = await Authenticate(context.RequestMessage, jwtService, authService);

            await next(context, instanceProvider);
        }
    }

    public class AuthResult
    {
        public static readonly AuthResult Anonymous = new AuthResult();
    }

    public class JwtPayload
    {
        public int SessionID { get; set; }
    }

    public class ProfileInfoRequest { }

    public class ProfileInfoResponse { }

    public class RegisterRequest
    {
        [Email]
        [Required(ErrorMessage = "The email field is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        [StringLength(10, MinimumLength = 40, ErrorMessage = "The password must be at least 10 and 40 characters long.")]
        public string Password { get; set; }
    }

    public class RegisterResponse
    {
        public string CsrfToken { get; set; }

        public string EmailAddress { get; set; }

        public int UserProfileID { get; set; }
    }

    public class LoginRequest
    {
        [Required(ErrorMessage = "The username field is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class LoginResponse
    {
        public string CsrfToken { get; set; }

        public string EmailAddress { get; set; }

        public int UserProfileID { get; set; }
    }
}
