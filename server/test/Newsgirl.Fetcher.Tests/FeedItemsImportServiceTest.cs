using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using NSubstitute;
using Xunit;
using Newsgirl.Testing;

namespace Newsgirl.Fetcher.Tests
{
    using Shared;

    public class FeedItemsImportServiceTest : DatabaseTest
    {
        [Fact]
        public async Task GetFeedsForUpdate_Returns_All_Feeds()
        {
            var log = new StructuredLogMock();

            var importService = new FeedItemsImportService(this.Db, DbConnection, log);

            var feeds = Enumerable.Range(1, 10)
                .Select(i => new FeedPoco
                {
                    FeedItemsHash = i,
                    FeedContentHash = i,
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
            var log = new StructuredLogMock();

            var importService = new FeedItemsImportService(this.Db, this.DbConnection, log);

            var feeds = Enumerable.Range(1, 10)
                .Select(i => new FeedPoco
                {
                    FeedName = $"feed {i}",
                    FeedUrl = $"url {i}",
                }).ToList();

            foreach (var feed in feeds)
            {
                await this.Db.Insert(feed);
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
                    NewFeedItemsHash = 1,
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
                    NewFeedItemsHash = 2,
                },
                new FeedUpdateModel
                {
                    Feed = feeds.First(x => x.FeedID == 3), 
                },
                new FeedUpdateModel
                {
                    Feed = feeds.First(x => x.FeedID == 4),
                    NewItems = new List<FeedItemPoco>(),
                    NewFeedItemsHash = 4
                },
                new FeedUpdateModel
                {
                    NewItems = Enumerable.Range(200, 10)
                        .Select(i => new FeedItemPoco
                        {
                            FeedID = 5,
                            FeedItemDescription = null,
                            FeedItemHash = i,
                            FeedItemTitle = $"title {i}",
                            FeedItemUrl = null,
                            FeedItemAddedTime = TestHelper.Date2000,
                        }).ToList(),
                    Feed = feeds.First(x => x.FeedID == 5),
                    NewFeedItemsHash = 5,
                },
            };
            
            await importService.ImportItems(updates.ToArray());

            var resultFeeds = this.Db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToList();
            var resultFeedItems = this.Db.Poco.FeedItems.OrderByDescending(x => x.FeedItemID).ToList();
            
            Snapshot.Match(new {resultFeeds, resultFeedItems});
        }
        
        [Fact]
        public async Task GetMissingFeedItems_Returns_Correct_Result()
        {
            var log = new StructuredLogMock();

            var importService = new FeedItemsImportService(this.Db, this.DbConnection, log);

            var feeds = Enumerable.Range(1, 10)
                .Select(i => new FeedPoco
                {
                    FeedName = $"feed {i}",
                    FeedUrl = $"url {i}",
                }).ToList();

            await this.Db.BulkInsert(feeds);

            var feedItems = Enumerable.Range(1, 10)
                .Select(i => new FeedItemPoco
                {
                    FeedID = 1,
                    FeedItemHash = i,
                    FeedItemDescription = $"desc {i}",
                    FeedItemTitle = $"title {i}",
                    FeedItemUrl = $"url {i}",
                    FeedItemAddedTime = TestHelper.Date2000,
                }).ToList();
            
            feedItems.AddRange(Enumerable.Range(30, 10)
                .Select(i => new FeedItemPoco
                {
                    FeedID = 2,
                    FeedItemHash = i,
                    FeedItemDescription = $"desc {i}",
                    FeedItemTitle = $"title {i}",
                    FeedItemUrl = $"url {i}",
                    FeedItemAddedTime = TestHelper.Date2000,
                }));

            await this.Db.BulkInsert(feedItems);

            long[] hashes = {
                1, 2, 3, 31, 32, 33
            };
            
            var missingFeedItems = await importService.GetMissingFeedItems(1, hashes);
            
            Array.Sort(missingFeedItems);
                        
            Snapshot.Match(missingFeedItems);
        }
    }
}
