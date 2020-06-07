namespace Newsgirl.Server.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Server;
    using Testing;
    using Xunit;

    public class InitializationTest
    {
        private const string RandomAddress = "http://127.0.0.1:0";

        private static async Task<HttpServerApp> CreateTestApp()
        {
            var app = new HttpServerApp
            {
                ErrorReporter = new ErrorReporterMock()
            };
            
            string appConfigPath = Path.GetFullPath("../../../newsgirl-server-test-config.json");
            Environment.SetEnvironmentVariable("APP_CONFIG_PATH", appConfigPath);

            await app.Start(RandomAddress);
            
            Assert.Equal(appConfigPath, app.AppConfigPath);
            
            return app;
        }

        [Fact]
        public async Task Server_Runs_Without_Error()
        {
            await using var app = await CreateTestApp();

            var shutdownTask = Task.Run(async () =>
            {
                await Task.Delay(100);
            
                app.TriggerShutdown();
            });
            
            await app.AwaitShutdownTrigger();

            await shutdownTask;
        }

        [Fact]
        public async Task IoC_Resolves_All_Registered_Types()
        {
            await using var app = await CreateTestApp();

            var ignored = new[]
            {
                typeof(ILifetimeScope),
                typeof(IComponentContext)
            };

            var registeredTypes = app.IoC.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .Select(x => ((TypedService) x).ServiceType)
                .Where(x => !ignored.Contains(x))
                .ToList();

            foreach (var registeredType in registeredTypes)
            {
                app.IoC.Resolve(registeredType);
            }
        }
        
        [Fact]
        public async Task Responds_to_request()
        {
            await using var app = await CreateTestApp();

            var client = new HttpClient
            {
                BaseAddress = new Uri(app.GetAddress())
            };
            
            var response = await client.PostAsync("/", new StringContent("{\"type\": \"PingRequest\", \"payload\":{} }"));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();
            
            Snapshot.MatchJson(responseBody);
        }
    }
}
