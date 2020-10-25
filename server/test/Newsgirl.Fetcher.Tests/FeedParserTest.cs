namespace Newsgirl.Fetcher.Tests
{
    using System.Threading.Tasks;
    using NSubstitute;
    using Shared;
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
            var hasher = new Hasher();

            var dateStub = Substitute.For<DateTimeService>();
            dateStub.EventTime().Returns(TestHelper.Date2000);
            dateStub.CurrentTime().Returns(TestHelper.Date2000);

            var parser = new FeedParser(hasher, dateStub, TestHelper.LogStub);

            string feedContent = await TestHelper.GetResourceText(resourceName);

            var parsedFeed = parser.Parse(feedContent);

            Snapshot.Match(parsedFeed, new[] {resourceName});
        }
    }
}
