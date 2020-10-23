namespace Newsgirl.Server
{
    using System.ComponentModel.DataAnnotations;
    using System.Threading.Tasks;
    using LinqToDB;
    using Shared;
    using Shared.Postgres;

    public class AuthHandler
    {
        private readonly IDbService db;
        private readonly RngProvider rngProvider;

        public AuthHandler(IDbService db, RngProvider rngProvider)
        {
            this.db = db;
            this.rngProvider = rngProvider;
        }

        [RpcBind(typeof(RegisterRequest), typeof(RegisterResponse))]
        public async Task<RpcResult<RegisterResponse>> Register(RegisterRequest req, RequestInfo requestInfo)
        {
            req.Email = req.Email.ToLowerInvariant();

            var existingLogin = await this.db.Poco.Logins.FirstOrDefaultAsync(x => x.Username == req.Email);

            if (existingLogin == null)
            {
                return "Username is already taken.";
            }

            await using (var tx = await this.db.BeginTransaction())
            {
                var profile = new UserProfilePoco
                {
                    EmailAddress = req.Email,
                    RegistrationDate = requestInfo.RequestTime,
                };

                await this.db.Save(profile);

                var login = new LoginPoco
                {
                    Username = req.Email,
                    Verified = false,
                    UserProfileID = profile.UserProfileID,
                    Password = BCryptHelper.CreatePassword(req.Password),
                    VerificationCode = this.rngProvider.GenerateSecureString(100),
                    // add registration date
                };

                await this.db.Save(login);

                await tx.CommitAsync();

                return new RegisterResponse();
            }
        }
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
}
