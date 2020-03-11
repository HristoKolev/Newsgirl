using System.Threading.Tasks;
using Newsgirl.Testing;
using Xunit;

namespace Newsgirl.Shared.Tests
{
    public class SystemSettingsServiceTest : DatabaseTest
    {
        [Fact]
        public async Task ReadSettings_Returns_Correct_Result()
        {
            var systemSettingsService = new SystemSettingsService(this.Db);

            var settings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
            
            Snapshot.Match(settings);
        }
    }
}
