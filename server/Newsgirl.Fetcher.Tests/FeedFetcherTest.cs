using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Newsgirl.Fetcher.Tests.Infrastructure;
using Newsgirl.Shared;
using Newsgirl.Shared.Data;
using NSubstitute;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedFetcherTest
    {
        [Fact]
        public async Task Returns_Correct_Result_Sequential()
        {
            var updates = await TestFeedFetcher(false);
            Snapshot.Match(updates);
        }
        
        [Fact]
        public async Task Returns_Correct_Result_Parallel()
        {
            var updates = await TestFeedFetcher(true);
            Snapshot.Match(updates);
        }

        private static async Task<FeedUpdateModel[]> TestFeedFetcher(bool parallelFetching)
        {
            var feeds = new List<FeedPoco>
            {
                new FeedPoco {FeedUrl = "fetcher-1.xml", FeedID = 1},
                new FeedPoco {FeedUrl = "fetcher-2.xml", FeedID = 2, FeedHash = 351563459839931092 },
                new FeedPoco {FeedUrl = "fetcher-3.xml", FeedID = 3},
            };

            var importService = Substitute.For<IFeedItemsImportService>();
            importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));
            importService.GetMissingFeedItems(default, default)
                .ReturnsForAnyArgs(info =>
                {
                    var arr = info.Arg<long[]>();

                    return arr.Skip(1).ToArray();
                });

            FeedUpdateModel[] updates = null;

            await importService.ImportItems(Arg.Do<FeedUpdateModel[]>(x => updates = x));

            var systemSettings = new SystemSettingsModel
            {
                ParallelFeedFetching = parallelFetching,
            };

            var contentProvider = Substitute.For<IFeedContentProvider>();
            contentProvider.GetFeedContent(null).ReturnsForAnyArgs(info =>
            {
                var feedPoco = info.Arg<FeedPoco>();
                
                return TestHelper.GetResource(feedPoco.FeedUrl);
            });

            var fetcher = new FeedFetcher(
                contentProvider,
                new FeedParser(new Hasher(), TestHelper.DateProviderStub, TestHelper.LogStub),
                importService,
                systemSettings,
                TestHelper.TransactionServiceStub,
                TestHelper.LogStub
            );

            await fetcher.FetchFeeds();

            return updates;
        }
    }
}
