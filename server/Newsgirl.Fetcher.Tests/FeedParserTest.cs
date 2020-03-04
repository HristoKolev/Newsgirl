using System.Threading.Tasks;
using Xunit;

using Newsgirl.Fetcher.Tests.Infrastructure;
using Newsgirl.Shared.Infrastructure;
using NSubstitute;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedParserTest
    {
        [Theory]
        [InlineData("yt-google-devs.xml")]
        [InlineData("AdventuresInAngular.rss")]
        [InlineData("test-case-1.xml")]
        public async Task For_A_Given_Feed_Content_Returns_The_Correct_Result(string resourceName)
        {
            var hasher = new Hasher();

            var dateStub = Substitute.For<IDateProvider>();
            dateStub.Now().Returns(TestHelper.Date2000);
            
            var parser = new FeedParser(hasher, dateStub);

            string feedContent = await TestHelper.GetResource(resourceName);
            
            var parsedFeed = parser.Parse(feedContent);

            Snapshot.Match(parsedFeed, new []{resourceName});
        }
    }
}
