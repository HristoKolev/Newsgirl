namespace Newsgirl.Server.Tests
{
    using System;
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
            builder.Register((c, p) => TestHelper.DateTimeServiceStub).SingleInstance();
            builder.RegisterType<RngServiceMock>().As<RngService>().SingleInstance();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>().SingleInstance();
            builder.RegisterType<StructuredLogMock>().As<ILog>().SingleInstance();
        }

        [Fact]
        public async Task RegisterReturnsErrorWhenTheUsernameIsTaken()
        {
            string email = "test@abc.de";
            string password = "test123";

            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(email, password);

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
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
        }

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

    public class AuthHandlerLoginReturnsAnErrorOnWrongUsername : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorOnWrongUsername()
        {
            string email = "test@abc.de";
            string password = "test123";

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = email,
                Password = password,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginReturnsAnErrorOnWrongPassword : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorOnWrongPassword()
        {
            string email = "test123@abc.de";
            string password = "test123";

            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(email, password);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = email,
                Password = password + "wrong",
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginReturnsAnErrorWhenTheLoginIsDisabled : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
        }

        [Fact]
        public async Task LoginReturnsAnErrorWhenTheLoginIsDisabled()
        {
            string email = "test123@abc.de";
            string password = "test123";

            var authService = this.App.IoC.Resolve<AuthService>();
            var (_, login) = await authService.CreateProfile(email, password);
            login.Enabled = false;
            await this.Db.Save(login);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = email,
                Password = password,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerLoginWorksInTheCorrectCase : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
        }

        [Fact]
        public async Task LoginWorksInTheCorrectCase()
        {
            string email = "test123@abc.de";
            string password = "test123";

            var authService = this.App.IoC.Resolve<AuthService>();
            await authService.CreateProfile(email, password);

            var result = await this.RpcClient.Login(new LoginRequest
            {
                Username = email,
                Password = password,
            });

            Snapshot.Match(result);
        }
    }

    public class AuthHandlerProfileInfoWorks : HttpServerAppTest
    {
        protected override void ConfigureMocks(ContainerBuilder builder)
        {
            builder.Register((c, p) => TestHelper.DateTimeServiceStub);
            builder.RegisterType<RngServiceMock>().As<RngService>();
            builder.RegisterType<PasswordServiceMock>().As<PasswordService>();
            builder.RegisterType<StructuredLogMock>().As<ILog>();
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
