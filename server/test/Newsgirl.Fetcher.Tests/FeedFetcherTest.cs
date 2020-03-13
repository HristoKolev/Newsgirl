using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Newsgirl.Shared;
using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;
using Newsgirl.Testing;
using NSubstitute;
using NSubstitute.ExceptionExtensions;

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
                new FeedPoco {FeedUrl = "fetcher-2.xml", FeedID = 2, FeedItemsHash = 351563459839931092, FeedContentHash = 123},
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

            var fetcher = new FeedFetcher(
                TestResourceContentProvider,
                new FeedParser(new Hasher(), TestHelper.DateProviderStub, TestHelper.LogStub),
                importService,
                new SystemSettingsModel
                {
                    ParallelFeedFetching = parallelFetching,
                },
                TestHelper.TransactionServiceStub,
                new Hasher(), 
                TestHelper.LogStub
            );

            await fetcher.FetchFeeds();

            return updates;
        }
        
        [Fact]
        public async Task Reports_When_The_Content_Provider_Throws()
        {
            var feeds = new List<FeedPoco>
            {
                new FeedPoco {FeedUrl = "fetcher-1.xml", FeedID = 1, FeedItemsHash = 351563459839931092, FeedContentHash = 123 },
            };

            var importService = Substitute.For<IFeedItemsImportService>();
            importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

            var contentProvider = Substitute.For<IFeedContentProvider>();
            contentProvider.GetFeedContent(null).ThrowsForAnyArgs(new ApplicationException());

            Exception err = null;
            
            var log = Substitute.For<ILog>();
            log.When(x => x.Debug(Arg.Any<Func<string>>())).Do(info => info.Arg<Func<string>>()());
            await log.Error(Arg.Do<DetailedLogException>(x => err = x), Arg.Any<Dictionary<string, object>>());

            var fetcher = new FeedFetcher(
                contentProvider,
                new FeedParser(new Hasher(), TestHelper.DateProviderStub, log),
                importService,
                new SystemSettingsModel(),
                TestHelper.TransactionServiceStub,
                new Hasher(), 
                log
            );

            await fetcher.FetchFeeds();

            Snapshot.MatchError(err);
        }
        
        
        [Fact]
        public async Task Reports_When_The_Parser_Throws()
        {
            var feeds = new List<FeedPoco>
            {
                new FeedPoco {FeedUrl = "fetcher-1.xml", FeedID = 1, FeedItemsHash = 351563459839931092, FeedContentHash = 123 },
            };

            var importService = Substitute.For<IFeedItemsImportService>();
            importService.GetFeedsForUpdate().Returns(Task.FromResult(feeds));

            Exception err = null;
            var log = Substitute.For<ILog>();
            await log.Error(Arg.Do<DetailedLogException>(x => err = x), Arg.Any<Dictionary<string, object>>());

            var feedParser = Substitute.For<IFeedParser>();
            feedParser.Parse(null).ThrowsForAnyArgs(new ApplicationException());
            
            var fetcher = new FeedFetcher(
                TestResourceContentProvider,
                feedParser,
                importService,
                new SystemSettingsModel(),
                TestHelper.TransactionServiceStub,
                new Hasher(), 
                log
            );

            await fetcher.FetchFeeds();

            Snapshot.MatchError(err);
        }
        
        public static IFeedContentProvider TestResourceContentProvider
        {
            get
            {
                var contentProvider = Substitute.For<IFeedContentProvider>();
                contentProvider.GetFeedContent(null)
                    .ReturnsForAnyArgs(info =>
                    {
                        var feedPoco = info.Arg<FeedPoco>();
                        var contentTask = TestHelper.GetResource(feedPoco.FeedUrl);

                        return contentTask.ContinueWith(x => Encoding.UTF8.GetBytes(x.Result));
                    });

                return contentProvider;
            }
        }

    }
}