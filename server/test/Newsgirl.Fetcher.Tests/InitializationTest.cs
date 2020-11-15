namespace Newsgirl.Fetcher.Tests
{
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Shared;
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
            var injectedConfig = JsonHelper.Deserialize<FetcherAppConfig>(await File.ReadAllTextAsync(appConfigPath));
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

            var container = app.IoC;

            var registeredTypes = container
                .ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .Cast<TypedService>()
                .Select(x => x.ServiceType)
                .Where(x => x != typeof(ILifetimeScope) && x != typeof(IComponentContext))
                .Distinct()
                .ToList();

            foreach (var registeredType in registeredTypes)
            {
                container.Resolve(registeredType);
            }
        }
    }
}
