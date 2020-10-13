// ReSharper disable AccessToDisposedClosure

namespace Newsgirl.Server.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Infrastructure;
    using Testing;
    using Xunit;

    public class HttpServerAppTest
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

                var response = await client.PostAsync($"/rpc/{nameof(PingRequest)}", new StringContent("{ \"payload\": {} }"));
                response.EnsureSuccessStatusCode();
                string responseBody = await response.Content.ReadAsStringAsync();

                Snapshot.MatchJson(responseBody);
            }
        }
    }
}
