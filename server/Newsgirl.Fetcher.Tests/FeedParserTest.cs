using System.Threading.Tasks;
using Xunit;

using Newsgirl.Fetcher.Tests.Infrastructure;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedParserTest
    {
        [Theory]
        [InlineData("yt-google-devs.txt")]
        [InlineData("AdventuresInAngular.rss")]
        [InlineData("test-case-1.txt")]
        public async Task For_A_Given_Feed_Content_Returns_The_Correct_Result(string resourceName)
        {
            var hasher = new Hasher();

            var dateProvider = new DateProviderStub(TestHelper.Date2000);
            
            var parser = new FeedParser(hasher, dateProvider);

            string feedContent = await TestHelper.GetResource(resourceName);
            
            var parsedFeed = parser.Parse(feedContent);

            Snapshot.Match(parsedFeed, new []{resourceName});
        }
    }
}
