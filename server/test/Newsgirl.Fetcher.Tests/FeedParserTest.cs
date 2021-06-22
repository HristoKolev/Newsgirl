namespace Newsgirl.Fetcher.Tests
{
    using System.Threading.Tasks;
    using Testing;
    using Xunit;

    public class FeedParserTest
    {
        [Theory]
        [InlineData("yt-google-devs.xml")]
        [InlineData("AdventuresInAngular.rss")]
        [InlineData("test-case-1.xml")]
        public async Task For_A_Given_Feed_Content_Returns_The_Correct_Result(string resourceName)
        {
            var parser = new FeedParser(TestHelper.DateTimeServiceStub, TestHelper.LogStub);

            string feedContent = await TestHelper.GetResourceText(resourceName);

            var parsedFeed = parser.Parse(feedContent, 1);

            Snapshot.Match(parsedFeed, new[] {resourceName});
        }
    }
}
