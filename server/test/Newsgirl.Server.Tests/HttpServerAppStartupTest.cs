// ReSharper disable AccessToDisposedClosure

namespace Newsgirl.Server.Tests
{
    using System;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Testing;
    using Xunit;

    public class HttpServerAppTestServerShutsDownCorrectly : HttpServerAppTest
    {
        [Fact]
        public async Task Server_Shuts_Down_Correctly()
        {
            var shutdownTask = Task.Run(async () =>
            {
                await Task.Delay(100);

                this.App.TriggerShutdown();
            });

            await this.App.AwaitShutdownTrigger();

            await shutdownTask;
        }
    }

    public class HttpServerAppTestIoCResolvesAllRegisteredTypes : HttpServerAppTest
    {
        [Fact]
        public void IoC_Resolves_All_Registered_Types()
        {
            var ignored = new[]
            {
                typeof(ILifetimeScope),
                typeof(IComponentContext),
            };

            var registeredTypes = this.App.IoC.ComponentRegistry.Registrations
                .SelectMany(x => x.Services)
                .Select(x => ((TypedService) x).ServiceType)
                .Where(x => !ignored.Contains(x))
                .ToList();

            foreach (var registeredType in registeredTypes)
            {
                this.App.IoC.Resolve(registeredType);
            }
        }
    }

    public class HttpServerAppTestRespondsToRequest : HttpServerAppTest
    {
        [Fact]
        public async Task Responds_to_request()
        {
            var client = new HttpClient
            {
                BaseAddress = new Uri(this.App.GetAddress()),
            };

            var response = await client.PostAsync($"/rpc/{nameof(PingRequest)}", new StringContent("{ \"payload\": {} }"));
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            Snapshot.MatchJson(responseBody);
        }
    }
}
