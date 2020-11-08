namespace Newsgirl.Fetcher.Tests
{
    using System.IO;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Testing;
    using Xunit;

    public class FetcherAppTest : AppDatabaseTest
    {
        private async Task<FetcherApp> CreateFetcherApp()
        {
            var app = new FetcherApp
            {
                ErrorReporter = new ErrorReporterMock(),
            };

            string appConfigPath = Path.GetFullPath("../../../newsgirl-fetcher-test-config.json");
            var injectedConfig = JsonSerializer.Deserialize<FetcherAppConfig>(await File.ReadAllTextAsync(appConfigPath));
            injectedConfig.ConnectionString = this.ConnectionString;
            app.InjectedAppConfig = injectedConfig;

            await app.Initialize();

            return app;
        }

        [Fact]
        public async Task Fetcher_Runs_Without_Error()
        {
            await using var app = await this.CreateFetcherApp();

            await app.RunCycle();
        }

        [Fact]
        public async Task IoC_Resolves_All_Registered_Types()
        {
            await using var app = await this.CreateFetcherApp();

            var ignored = new[]
            {
                typeof(ILifetimeScope),
                typeof(IComponentContext),
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
    }
}
