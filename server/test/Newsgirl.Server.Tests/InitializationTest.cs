namespace Newsgirl.Server.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Testing;
    using Xunit;

    public class InitializationTest
    {
        [Fact]
        public async Task Server_Shuts_Down_Correctly()
        {
            await using (var tester = await HttpServerAppTester.Create())
            {
                var shutdownTask = Task.Run(async () =>
                {
                    await Task.Delay(100);

                    tester.App.TriggerShutdown();
                });

                await tester.App.AwaitShutdownTrigger();

                await shutdownTask;
            }
        }

        [Fact]
        public async Task IoC_Resolves_All_Registered_Types()
        {
            await using (var tester = await HttpServerAppTester.Create())
            {
                var ignored = new[]
                {
                    typeof(ILifetimeScope),
                    typeof(IComponentContext),
                };

                var registeredTypes = tester.App.IoC.ComponentRegistry.Registrations
                    .SelectMany(x => x.Services)
                    .Select(x => ((TypedService) x).ServiceType)
                    .Where(x => !ignored.Contains(x))
                    .ToList();

                foreach (var registeredType in registeredTypes)
                {
                    tester.App.IoC.Resolve(registeredType);
                }
            }
        }

        [Fact]
        public async Task Responds_to_request()
        {
            await using (var tester = await HttpServerAppTester.Create())
            {
                var client = new HttpClient
                {
                    BaseAddress = new Uri(tester.App.GetAddress()),
                };

                var response = await client.PostAsync($"/rpc/{nameof(PingRequest)}", new StringContent("{}"));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Snapshot.MatchJson(responseBody);
            }
        }
    }

    public class HttpServerAppTester : IAsyncDisposable
    {
        public HttpServerApp App { get; private set; }

        public static async Task<HttpServerAppTester> Create()
        {
            var tester = new HttpServerAppTester();

            var app = new HttpServerApp();

            TaskScheduler.UnobservedTaskException += tester.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += tester.OnUnhandledException;

            Assert.Null(app.Log);
            Assert.Null(app.AppConfig);
            Assert.Null(app.AsyncLocals);
            Assert.Null(app.ErrorReporter);
            Assert.Null(app.IoC);
            Assert.Null(app.RpcEngine);
            Assert.Null(app.SystemSettings);
            Assert.Null(app.AppConfigPath);
            Assert.False(app.Started);

            app.ErrorReporter = new ErrorReporterMock();

            string appConfigPath = Path.GetFullPath("../../../newsgirl-server-test-config.json");
            Environment.SetEnvironmentVariable("APP_CONFIG_PATH", appConfigPath);

            await app.Start("http://127.0.0.1:0");

            Assert.Equal(appConfigPath, app.AppConfigPath);

            Assert.NotNull(app.Log);
            Assert.NotNull(app.AppConfig);
            Assert.NotNull(app.AsyncLocals);
            Assert.NotNull(app.ErrorReporter);
            Assert.NotNull(app.IoC);
            Assert.NotNull(app.RpcEngine);
            Assert.NotNull(app.SystemSettings);
            Assert.True(app.Started);

            tester.App = app;

            return tester;
        }

        private async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            await this.App.ErrorReporter.Error(e.Exception?.InnerException);
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
            Assert.Null(this.App.AsyncLocals);
            Assert.Null(this.App.ErrorReporter);
            Assert.Null(this.App.IoC);
            Assert.Null(this.App.RpcEngine);
            Assert.Null(this.App.SystemSettings);
            Assert.Null(this.App.AppConfigPath);
            Assert.False(this.App.Started);

            TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
        }
    }
}
