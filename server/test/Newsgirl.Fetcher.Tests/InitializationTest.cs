using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

using Autofac;
using Autofac.Core;
using Xunit;

using Newsgirl.Shared;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher.Tests
{
    public class InitializationTest
    {
        [Fact]
        public void IoC_Resolves_All_Registered_Types()
        {
            Global.AppConfig = new AppConfig
            {
                ConnectionString = "Server=test;Port=1234;Database=test;Uid=test;Pwd=test;",
                Logging = new CustomLoggerConfig
                {
                    DisableSentryIntegration = true,
                    SentryDsn = "http://123@home-sentry.lan/5"
                },
            };
            
            Global.SystemSettings = new SystemSettingsModel
            {
                HttpClientRequestTimeout = 123,
                HttpClientUserAgent = "123"
            };
            
            Global.Log = new CustomLogger(Global.AppConfig.Logging);

            var ignored = new[]
            {
                typeof(ILifetimeScope),
                typeof(IComponentContext),
            };
            
            using (var container = IoCFactory.Create())
            {
                var registeredTypes = container.ComponentRegistry.Registrations
                    .SelectMany(x => x.Services)
                    .Select(x => ((TypedService)x).ServiceType)
                    .Where(x => !ignored.Contains(x))
                    .ToList();

                foreach (var registeredType in registeredTypes)
                {
                    container.Resolve(registeredType);
                }
            }
        }
        
        [Fact]
        public async Task Fetcher_Runs_Without_Error()
        {
            string appConfigPath = Path.GetFullPath("../../../newsgirl-fetcher-test-config.json");
            Environment.SetEnvironmentVariable("APP_CONFIG_PATH", appConfigPath);
            Assert.Equal(appConfigPath, Global.AppConfigPath);

            await Global.InitializeAsync();

            await Global.RunCycleAsync();

            await Global.DisposeAsync();
        }
    }
}
