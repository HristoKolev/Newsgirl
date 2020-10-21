namespace Newsgirl.Server.Tests
{
    using System.Threading.Tasks;
    using Infrastructure;
    using Testing;

    public class HttpServerAppTest : AppDatabaseTest
    {
        private HttpServerAppTester tester;

        protected HttpServerApp App => this.tester.App;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();
            this.tester = await HttpServerAppTester.Create(this.ConnectionString);
        }

        public override async Task DisposeAsync()
        {
            await this.tester.DisposeAsync();
            await base.DisposeAsync();
        }
    }
}
