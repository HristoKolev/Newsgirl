using System.Threading.Tasks;
using Newsgirl.Shared;
using Xunit;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedContentProviderTest
    {
        [Theory]
        [InlineData("https://www.youtube.com/feeds/videos.xml?user=GoogleDevelopers")]
        [InlineData("https://v8project.blogspot.com/feeds/posts/default")]
        public async Task HTTP_Request_Returns_Correct_Result(string feedUrl)
        {
            var feed = new FeedPoco
            {
                FeedUrl = feedUrl
            };
            
            var contentProvider = new FeedContentProvider(new SystemSettingsModel
            {
                HttpClientRequestTimeout = 60,
                HttpClientUserAgent = "Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36"
            });

            byte[] bytes = await contentProvider.GetFeedContent(feed);

            string content = EncodingHelper.UTF8.GetString(bytes);

            Assert.Contains("<feed", content);
        }
    }
}