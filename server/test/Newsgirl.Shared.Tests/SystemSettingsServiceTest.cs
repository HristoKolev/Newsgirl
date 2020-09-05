namespace Newsgirl.Shared.Tests
{
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class SystemSettingsServiceTest : DatabaseTest
    {
        [Fact]
        public async Task ReadSettings_Returns_Correct_Result()
        {
            var systemSettingsService = new SystemSettingsService(this.Db);

            var settings = await systemSettingsService.ReadSettings<SystemSettingsModel>();

            Snapshot.Match(settings);
        }

        public SystemSettingsServiceTest() : base("before-app-tests.sql") { }
    }
}
