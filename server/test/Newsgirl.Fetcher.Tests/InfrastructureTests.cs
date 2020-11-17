namespace Newsgirl.Fetcher.Tests
{
    using System.Linq;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Xunit;

    public class FetcherAppTestRunCycleRunsWithoutError : FetcherAppTest
    {
        [Fact]
        public async Task Fetcher_Runs_Without_Error()
        {
            await this.App.RunCycle();
        }
    }

    public class FetcherAppTestIoCResolvesAllRegisteredTypes : FetcherAppTest
    {
        [Fact]
        public void IoC_Resolves_All_Registered_Types()
        {
            var container = this.App.IoC;

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
