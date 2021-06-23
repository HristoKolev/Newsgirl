namespace Newsgirl.Server.Tests
{
    using System.Threading.Tasks;
    using Autofac;
    using LinqToDB;
    using Shared;
    using Shared.Logging;
    using Testing;
    using Xunit;

    public class AuthHandlerRegisterReturnsErrorWhenTheUsernameIsTaken : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub).InstancePerLifetimeScope();
            builder.RegisterType<RngServiceMock>().As<RngService>().InstancePerLifetimeScope();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>().InstancePerLifetimeScope();
            builder.RegisterType<StructuredLogMock>().As<Log>().InstancePerLifetimeScope();
        }

        [Fact]
        public async Task RegisterReturnsErrorWhenTheUsernameIsTaken()
        {
            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(TEST_EMAIL, TEST_PASSWORD);

            var result = await this.RpcClient.Register(new RegisterRequest
            {
                Email = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerRegisterReturnsSuccessWhenANewProfileIsCreated : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task RegisterReturnsSuccessWhenANewProfileIsCreated()
        {
            var result = await this.RpcClient.Register(new RegisterRequest
            {
                Email = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            var profile = await this.Db.Poco.UserProfiles.FirstOrDefaultAsync(x => x.EmailAddress == TEST_EMAIL);

            var login = await this.Db.Poco.UserLogins.FirstOrDefaultAsync(x => x.UserProfileID == profile.UserProfileID);

            Snapshot.Match(new
            {
                result,
                profile,
                login,
            });
        }
    }

    public class AuthHandlerLoginReturnsAnErrorOnWrongUsername : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorOnWrongUsername()
        {
            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginReturnsAnErrorOnWrongPassword : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorOnWrongPassword()
        {
            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(TEST_EMAIL, TEST_PASSWORD);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = TEST_EMAIL,
                Password = TEST_PASSWORD + "wrong",
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginReturnsAnErrorWhenTheLoginIsDisabled : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorWhenTheLoginIsDisabled()
        {
            var authService = this.App.IoC.Resolve<AuthService>();
            var (_, login) = await authService.CreateProfile(TEST_EMAIL, TEST_PASSWORD);
            login.Enabled = false;
            await this.Db.Save(login);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginWorksInTheCorrectCase : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task LoginWorksInTheCorrectCase()
        {
            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(TEST_EMAIL, TEST_PASSWORD);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerProfileInfoWorks : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((_, _) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<Log>();
        }

        [Fact]
        public async Task ProfileInfoWorks()
        {
            await this.CreateProfile();

            var result = await this.RpcClient.ProfileInfo(new ProfileInfoRequest());

            Snapshot.Match(result);
        }
    }
}
