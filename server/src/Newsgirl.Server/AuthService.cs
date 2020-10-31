namespace Newsgirl.Server
{
    using System;
    using System.Threading.Tasks;
    using LinqToDB;
    using Shared;
    using Shared.Postgres;

    public class AuthService
    {
        private readonly IDbService db;
        private readonly PasswordService passwordService;
        private readonly RngService rngService;
        private readonly DateTimeService dateTimeService;

        public AuthService(
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

        public async Task<UserSessionPoco> CreateSession(int loginID, bool rememberMe)
        {
            var session = new UserSessionPoco
            {
                LoginDate = this.dateTimeService.EventTime(),
                LoginID = loginID,
                ExpirationDate = rememberMe ? (DateTime?) null : this.dateTimeService.EventTime().AddHours(3),
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
    }
}
