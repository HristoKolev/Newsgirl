namespace Newsgirl.Server
{
    using System;
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

        public AuthHandler(IDbService db, PasswordService passwordService, AuthService authService, JwtService jwtService)
        {
            this.db = db;
            this.passwordService = passwordService;
            this.authService = authService;
            this.jwtService = jwtService;
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
                var session = await this.authService.CreateProfile(req.Email, req.Password);

                await tx.CommitAsync();
                return new RegisterResponse();
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

            if (!this.passwordService.VerifyPassword(req.Password, login.PasswordHash))
            {
                return "Wrong username or password.";
            }

            login.PasswordHash = null;
            login.VerificationCode = null;

            await using (var tx = await this.db.BeginTransaction())
            {
                var session = await this.authService.CreateSession(login, req.RememberMe);

                await tx.CommitAsync();
                return new LoginResponse();
            }
        }
    }

    public class AuthService
    {
        private readonly IDbService db;
        private readonly PasswordService passwordService;
        private readonly RngService rngService;
        private readonly DateTimeService dateTimeService;

        public AuthService(IDbService db, PasswordService passwordService, RngService rngService, DateTimeService dateTimeService)
        {
            this.db = db;
            this.passwordService = passwordService;
            this.rngService = rngService;
            this.dateTimeService = dateTimeService;
        }

        public Task<UserLoginPoco> FindLogin(string username)
        {
            return this.db.Poco.UserLogins.FirstOrDefaultAsync(x => x.Username == username);
        }

        public async Task<UserSession> CreateSession(UserLoginPoco login, bool rememberMe)
        {
            var profile = await this.db.Poco.UserProfiles.FirstOrDefaultAsync(x => x.UserProfileID == login.UserProfileID);

            var session = new UserSessionPoco
            {
                LoginDate = this.dateTimeService.EventTime(),
                LoginID = login.LoginID,
                ExpirationDate = rememberMe ? (DateTime?) null : this.dateTimeService.EventTime().AddHours(3),
                CsrfToken = this.rngService.GenerateSecureString(40),
            };

            await this.db.Save(session);

            return new UserSession
            {
                Login = login,
                Profile = profile,
                Session = session,
            };
        }

        public async Task<UserSession> CreateProfile(string email, string password)
        {
            var profile = new UserProfilePoco
            {
                EmailAddress = email,
                RegistrationDate = this.dateTimeService.EventTime(),
            };

            await this.db.Save(profile);

            var login = new UserLoginPoco
            {
                UserProfileID = profile.UserProfileID,
                Username = email,
                Enabled = true,
                PasswordHash = this.passwordService.HashPassword(password),
                VerificationCode = this.rngService.GenerateSecureString(100),
                Verified = false,
            };

            await this.db.Save(login);

            var session = new UserSessionPoco
            {
                LoginDate = this.dateTimeService.EventTime(),
                LoginID = login.LoginID,
                ExpirationDate = this.dateTimeService.EventTime().AddHours(3),
                CsrfToken = this.rngService.GenerateSecureString(40),
            };

            await this.db.Save(session);

            return new UserSession
            {
                Login = login,
                Profile = profile,
                Session = session,
            };
        }
    }

    public class UserSession
    {
        public UserSessionPoco Session { get; set; }

        public UserLoginPoco Login { get; set; }

        public UserProfilePoco Profile { get; set; }
    }

    public class RegisterRequest
    {
        [Email]
        [Required(ErrorMessage = "The email field is required.")]
        public string Email { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        [StringLength(10, MinimumLength = 40, ErrorMessage = "The password must be at least 10 and 40 characters long.")]
        public string Password { get; set; }
    }

    public class RegisterResponse { }

    public class LoginRequest
    {
        [Required(ErrorMessage = "The username field is required.")]
        public string Username { get; set; }

        [Required(ErrorMessage = "The password field is required.")]
        public string Password { get; set; }

        public bool RememberMe { get; set; }
    }

    public class LoginResponse { }
}
