using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NSubstitute;
using Xunit;

using Newsgirl.Fetcher.Tests.Infrastructure;
using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher.Tests
{
    public class FeedItemsImportServiceTest : DatabaseTest
    {
        [Fact]
        public async Task GetFeedsForUpdate_Returns_All_Feeds()
        {
            var log = Substitute.For<ILog>();

            var importService = new FeedItemsImportService(this.Db, DbConnection, log);

            var feeds = Enumerable.Range(1, 10)
                .Select(i => new FeedPoco
                {
                    FeedHash = i,
                    FeedName = $"feed {i}",
                    FeedUrl = $"url {i}",
                }).ToList();

            await this.Db.BulkInsert(feeds);
            
            var resultFeeds = await importService.GetFeedsForUpdate();
            
            Snapshot.Match(resultFeeds);
        }
        
        
        [Fact]
        public async Task ImportItems_Copies_Items_Correctly()
        {
            var log = Substitute.For<ILog>();

            var importService = new FeedItemsImportService(this.Db, this.DbConnection, log);

            var feeds = Enumerable.Range(1, 10)
                .Select(i => new FeedPoco
                {
                    FeedName = $"feed {i}",
                    FeedUrl = $"url {i}",
                }).ToList();

            foreach (var feed in feeds)
            {
                await Db.Insert(feed);
            }

            var updates = new List<FeedUpdateModel>
            {
                new FeedUpdateModel
                {
                    NewItems = Enumerable.Range(1, 10)
                        .Select(i => new FeedItemPoco
                        {
                            FeedID = 1,
                            FeedItemDescription = $"desc {i}",
                            FeedItemHash = i,
                            FeedItemTitle = $"title {i}",
                            FeedItemUrl = $"url {i}",
                            FeedItemAddedTime = TestHelper.Date2000,
                        }).ToList(),
                    Feed = feeds.First(x => x.FeedID == 1),
                    NewFeedHash = 1,
                },
                new FeedUpdateModel
                {
                    NewItems = Enumerable.Range(100, 10)
                        .Select(i => new FeedItemPoco
                        {
                            FeedID = 2,
                            FeedItemDescription = $"desc {100 + i}",
                            FeedItemHash = i,
                            FeedItemTitle = $"title {100 + i}",
                            FeedItemUrl = $"url {100 + i}",
                            FeedItemAddedTime = TestHelper.Date2000,
                        }).ToList(),
                    Feed = feeds.First(x => x.FeedID == 2),
                    NewFeedHash = 2,
                },
                new FeedUpdateModel
                {
                    Feed = feeds.First(x => x.FeedID == 3), 
                },
                new FeedUpdateModel
                {
                    Feed = feeds.First(x => x.FeedID == 4),
                    NewItems = new List<FeedItemPoco>(),
                    NewFeedHash = 4
                }
            };
            
            await importService.ImportItems(updates.ToArray());

            var resultFeeds = Db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToList();
            var resultFeedItems = Db.Poco.FeedItems.OrderByDescending(x => x.FeedItemID).ToList();
            
            Snapshot.Match(new {resultFeeds, resultFeedItems});
        }
    }
}