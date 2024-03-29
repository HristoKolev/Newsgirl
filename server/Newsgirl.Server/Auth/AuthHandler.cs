namespace Newsgirl.Server.Auth;

using System.ComponentModel.DataAnnotations;
using System.Threading.Tasks;
using Infrastructure;
using LinqToDB;
using Shared;
using Xdxd.DotNet.Rpc;
using Xdxd.DotNet.Shared;

[RpcAuth(RequiresAuthentication = false)]
public class AuthHandler
{
    private readonly IDbService db;
    private readonly PasswordService passwordService;
    private readonly AuthService authService;
    private readonly JwtService jwtService;
    private readonly DateTimeService dateTimeService;
    private readonly HttpRequestState httpRequestState;

    public AuthHandler(
        IDbService db,
        PasswordService passwordService,
        AuthService authService,
        JwtService jwtService,
        DateTimeService dateTimeService,
        HttpRequestState httpRequestState)
    {
        this.db = db;
        this.passwordService = passwordService;
        this.authService = authService;
        this.jwtService = jwtService;
        this.dateTimeService = dateTimeService;
        this.httpRequestState = httpRequestState;
    }

    [RpcBind(typeof(RegisterRequest), typeof(RegisterResponse))]
    public async Task<Result<RegisterResponse>> Register(RegisterRequest req)
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

            var session = await this.authService.CreateSession(login.LoginID, profile.UserProfileID, false);

            var result = new Result<RegisterResponse>
            {
                Payload = new RegisterResponse
                {
                    CsrfToken = session.CsrfToken,
                    EmailAddress = profile.EmailAddress,
                    UserProfileID = profile.UserProfileID,
                },
            };

            await tx.CommitAsync();

            this.httpRequestState.HttpContext.Response
                .Headers["Set-Cookie"] = this.FormatCookie(session);

            return result;
        }
    }

    [RpcBind(typeof(LoginRequest), typeof(LoginResponse))]
    public async Task<Result<LoginResponse>> Login(LoginRequest req)
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
            var session = await this.authService.CreateSession(login.LoginID, profile.UserProfileID, req.RememberMe);

            var result = new Result<LoginResponse>
            {
                Payload = new LoginResponse
                {
                    CsrfToken = session.CsrfToken,
                    EmailAddress = profile.EmailAddress,
                    UserProfileID = profile.UserProfileID,
                },
            };

            await tx.CommitAsync();

            this.httpRequestState.HttpContext.Response
                .Headers["Set-Cookie"] = this.FormatCookie(session);

            return result;
        }
    }

    private string FormatCookie(UserSessionPoco session)
    {
        var now = this.dateTimeService.EventTime();

        var expirationDate = session.ExpirationDate ?? now.AddYears(1000);

        var jwtPayload = new JwtPayload
        {
            SessionID = session.SessionID,
            JwtID = session.SessionID,
            ExpirationTime = expirationDate.ToUnixEpochTime(),
            IssuedAt = now.ToUnixEpochTime(),
            NotBefore = now.ToUnixEpochTime(),
        };

        string jwt = this.jwtService.EncodeSession(jwtPayload);

        string cookie = $"jwt={jwt}; Expires={expirationDate:R}; Path=/; Secure; HttpOnly";

        return cookie;
    }
}

public class RegisterRequest
{
    [Email]
    [Required(ErrorMessage = "The email field is required.")]
    public string Email { get; set; }

    [Required(ErrorMessage = "The password field is required.")]
    [StringLength(40, MinimumLength = 10, ErrorMessage = "The password must be at least 10 and 40 characters long.")]
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

public interface AuthService
{
    Task<UserLoginPoco> FindLogin(string username);

    Task<UserSessionPoco> CreateSession(int loginID, int userProfileID, bool rememberMe);

    Task<(UserProfilePoco, UserLoginPoco)> CreateProfile(string email, string password);

    Task<UserSessionPoco> GetSession(int sessionID);
}

public class AuthServiceImpl : AuthService
{
    private readonly IDbService db;
    private readonly PasswordService passwordService;
    private readonly RngService rngService;
    private readonly DateTimeService dateTimeService;

    public AuthServiceImpl(
        IDbService db,
        PasswordService passwordService,
        RngService rngService,
        DateTimeService dateTimeService)
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

    public async Task<UserSessionPoco> CreateSession(int loginID, int userProfileID, bool rememberMe)
    {
        var session = new UserSessionPoco
        {
            LoginDate = this.dateTimeService.EventTime(),
            LoginID = loginID,
            ProfileID = userProfileID,
            ExpirationDate = rememberMe ? null : this.dateTimeService.EventTime().AddHours(3),
            CsrfToken = this.rngService.GenerateSecureString(40),
        };

        await this.db.Save(session);

        return session;
    }

    public async Task<(UserProfilePoco, UserLoginPoco)> CreateProfile(string email, string password)
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

        return (profile, login);
    }

    public Task<UserSessionPoco> GetSession(int sessionID)
    {
        return this.db.Poco.UserSessions.FirstOrDefaultAsync(x => x.SessionID == sessionID);
    }
}
