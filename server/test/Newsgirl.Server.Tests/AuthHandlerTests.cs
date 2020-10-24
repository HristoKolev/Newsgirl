namespace Newsgirl.Server.Tests
{
    using System.Threading.Tasks;
    using LinqToDB;
    using Shared;
    using Testing;
    using Xunit;

    public class AuthHandlerRegisterReturnsErrorWhenTheUsernameIsTaken : HttpServerAppTest
    {
        [Fact]
        public async Task RegisterReturnsErrorWhenTheUsernameIsTaken()
        {
            string email = "test@abc.de";
            string password = "test123";

            var profile = new UserProfilePoco
            {
                EmailAddress = email,
                RegistrationDate = TestHelper.Date2000,
            };

            await this.Db.Insert(profile);

            var login = new UserLoginPoco
            {
                Password = password,
                Username = email,
                Verified = true,
                RegistrationDate = TestHelper.Date2000,
                UserProfileID = profile.UserProfileID,
            };

            await this.Db.Insert(login);

            var result = await this.RpcClient.Register(new RegisterRequest
            {
                Email = email,
                Password = password,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerRegisterReturnsSuccessWhenANewProfileIsCreated : HttpServerAppTest
    {
        [Fact]
        public async Task RegisterReturnsSuccessWhenANewProfileIsCreated()
        {
            string email = "test@abc.de";
            string password = "test123";

            var result = await this.RpcClient.Register(new RegisterRequest
            {
                Email = email,
                Password = password,
            });

            var profile = await this.Db.Poco.UserProfiles.FirstOrDefaultAsync(x => x.EmailAddress == email);

            var login = await this.Db.Poco.UserLogins.FirstOrDefaultAsync(x => x.UserProfileID == profile.UserProfileID);

            Snapshot.Match(new
            {
                result,
                profile,
                login,
            });
        }
    }
}
