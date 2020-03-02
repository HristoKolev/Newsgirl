using System.Threading.Tasks;
using ApprovalTests;
using Newsgirl.Fetcher.Tests.Infrastructure;
using Newtonsoft.Json;
using Xunit;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedParserTest
    {
        [Theory]
        [InlineData("feed_content.txt")]
        public async Task Returns_Correct_Result(string resourceName)
        {
            var hasher = new Hasher();
            
            var appConfig = new AppConfig
            {
                Debug = new DebugConfig
                {
                    General = false
                }
            };
            
            var parser = new FeedParser(hasher, appConfig);

            string feedContent = await TestHelper.GetResource(resourceName);
            
            var parsedFeed = parser.Parse(feedContent);

            Approvals.VerifyJson(JsonConvert.SerializeObject(parsedFeed));
        }
    }
}
