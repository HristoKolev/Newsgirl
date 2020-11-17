namespace Newsgirl.Fetcher.Tests
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Shared;
    using Testing;
    using Xunit;

    public class FetcherAppTest : AppDatabaseTest
    {
        private FetcherAppTester tester;

        protected FetcherApp App => this.tester.App;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var mockModule = new FunctionAutofacModule(this.ConfigureMocks);

            this.tester = await FetcherAppTester.Create(this.ConnectionString, mockModule);
        }

        public override async Task DisposeAsync()
        {
            await this.tester.DisposeAsync();
            await base.DisposeAsync();
        }

        protected virtual void ConfigureMocks(ContainerBuilder builder) { }
    }

    public class FetcherAppTester : IAsyncDisposable
    {
        public FetcherApp App { get; private set; }

        public static async Task<FetcherAppTester> Create(string connectionString, Module mockModule)
        {
            var tester = new FetcherAppTester();

            var app = new FetcherApp
            {
                InjectedIoCModule = mockModule,
            };

            TaskScheduler.UnobservedTaskException += tester.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += tester.OnUnhandledException;

            Assert.Null(app.Log);
            Assert.Null(app.AppConfig);
            Assert.Null(app.ErrorReporter);
            Assert.Null(app.IoC);
            Assert.Null(app.SystemSettings);

            app.ErrorReporter = new ErrorReporterMock();

            string appConfigPath = Path.GetFullPath("../../../newsgirl-fetcher-test-config.json");
            var injectedConfig = JsonHelper.Deserialize<FetcherAppConfig>(await File.ReadAllTextAsync(appConfigPath));
            injectedConfig.ConnectionString = connectionString;
            app.InjectedAppConfig = injectedConfig;

            await app.Initialize();

            Assert.NotNull(app.Log);
            Assert.NotNull(app.AppConfig);
            Assert.NotNull(app.ErrorReporter);
            Assert.NotNull(app.IoC);
            Assert.NotNull(app.SystemSettings);

            tester.App = app;

            return tester;
        }

        private async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            await this.App.ErrorReporter.Error(e.Exception!.InnerException);
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await this.App.ErrorReporter.Error((Exception) e.ExceptionObject);
        }

        public async ValueTask DisposeAsync()
        {
            await this.App.DisposeAsync();

            Assert.Null(this.App.Log);
            Assert.Null(this.App.AppConfig);
            Assert.Null(this.App.ErrorReporter);
            Assert.Null(this.App.IoC);
            Assert.Null(this.App.SystemSettings);

            TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
        }
    }
}
