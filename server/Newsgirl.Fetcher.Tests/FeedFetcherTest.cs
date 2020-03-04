using System.Collections.Generic;
using System.Threading.Tasks;
using Newsgirl.Fetcher.Tests.Infrastructure;
using Newsgirl.Shared;
using Newsgirl.Shared.Data;
using NSubstitute;
using Xunit;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedFetcherTest
    {
        [Theory]
        [InlineData("fetcher-1.xml")]
        public async Task Returns_Correct_Result(string resourceName)
        {
            var feeds = new List<FeedPoco>
            {
                new FeedPoco()
            };
            
            var importService = Substitute.For<IFeedItemsImportService>();
            importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

            var systemSettings = new SystemSettingsModel();
                
            var contentProvider = Substitute.For<IFeedContentProvider>();
            string feedContent = await TestHelper.GetResource(resourceName);
            contentProvider.GetFeedContent(Arg.Any<FeedPoco>()).Returns(Task.FromResult(feedContent));

            var fetcher = new FeedFetcher(
                contentProvider, 
                new FeedParser(new Hasher(), TestHelper.DateProviderStub),
                importService,
                systemSettings,
                TestHelper.TransactionServiceStub
            );
        }
    }
}
