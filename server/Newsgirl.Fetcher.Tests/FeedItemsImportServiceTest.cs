namespace Newsgirl.Fetcher.Tests;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared;
using Xdxd.DotNet.Testing;
using Xunit;

public class FeedItemsImportServiceTest : AppDatabaseTest
{
    [Fact]
    public async Task GetFeedsForUpdate_Returns_All_Feeds()
    {
        var importService = new FeedItemsImportService(this.Db);

        var feeds = Enumerable.Range(1, 10).Select(i => new FeedPoco
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
        var importService = new FeedItemsImportService(this.Db);

        var feeds = Enumerable.Range(1, 10).Select(i => new FeedPoco
        {
            FeedName = $"feed {i}",
            FeedUrl = $"url {i}",
        }).ToList();

        // Insert one by one in order to set the PK value.
        foreach (var feed in feeds)
        {
            await this.Db.Insert(feed);
        }

        var updates = new List<FeedUpdateModel>
        {
            new FeedUpdateModel
            {
                NewItems = Enumerable.Range(1, 10).Select(i => new FeedItemPoco
                {
                    FeedID = 1,
                    FeedItemDescription = $"desc {i}",
                    FeedItemStringID = $"string id {i}",
                    FeedItemStringIDHash = i,
                    FeedItemTitle = $"title {i}",
                    FeedItemUrl = $"url {i}",
                    FeedItemAddedTime = StubHelper.Date3000,
                }).ToList(),
                Feed = feeds.First(x => x.FeedID == 1),
                NewFeedItemsHash = 1,
                NewFeedContentHash = 1,
            },
            new FeedUpdateModel
            {
                NewItems = Enumerable.Range(100, 10).Select(i => new FeedItemPoco
                {
                    FeedID = 2,
                    FeedItemDescription = $"desc {100 + i}",
                    FeedItemStringID = $"string id {i}",
                    FeedItemStringIDHash = i,
                    FeedItemTitle = $"title {100 + i}",
                    FeedItemUrl = $"url {100 + i}",
                    FeedItemAddedTime = StubHelper.Date3000,
                }).ToList(),
                Feed = feeds.First(x => x.FeedID == 2),
                NewFeedItemsHash = 2,
                NewFeedContentHash = 2,
            },
            new FeedUpdateModel
            {
                NewItems = Enumerable.Range(200, 10).Select(i => new FeedItemPoco
                {
                    FeedID = 3,
                    FeedItemDescription = null,
                    FeedItemStringID = $"string id {i}",
                    FeedItemStringIDHash = i,
                    FeedItemTitle = $"title {i}",
                    FeedItemUrl = null,
                    FeedItemAddedTime = StubHelper.Date3000,
                }).ToList(),
                Feed = feeds.First(x => x.FeedID == 3),
                NewFeedItemsHash = 3,
                NewFeedContentHash = 3,
            },
        };

        foreach (var update in updates)
        {
            await importService.ApplyUpdate(update);
        }

        var resultFeeds = this.Db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToList();
        var resultFeedItems = this.Db.Poco.FeedItems.OrderByDescending(x => x.FeedItemID).ToList();

        Snapshot.Match(new { resultFeeds, resultFeedItems });
    }

    [Fact]
    public async Task GetMissingFeedItems_Returns_Correct_Result()
    {
        var importService = new FeedItemsImportService(this.Db);

        var feeds = Enumerable.Range(1, 10).Select(i => new FeedPoco
        {
            FeedName = $"feed {i}",
            FeedUrl = $"url {i}",
        }).ToList();

        await this.Db.BulkInsert(feeds);

        var feedItems = Enumerable.Range(1, 10).Select(i => new FeedItemPoco
        {
            FeedID = 1,
            FeedItemStringID = $"string id {i}",
            FeedItemStringIDHash = i,
            FeedItemDescription = $"desc {i}",
            FeedItemTitle = $"title {i}",
            FeedItemUrl = $"url {i}",
            FeedItemAddedTime = StubHelper.Date3000,
        }).ToList();

        feedItems.AddRange(Enumerable.Range(30, 10).Select(i => new FeedItemPoco
        {
            FeedID = 2,
            FeedItemStringID = $"string id {i}",
            FeedItemStringIDHash = i,
            FeedItemDescription = $"desc {i}",
            FeedItemTitle = $"title {i}",
            FeedItemUrl = $"url {i}",
            FeedItemAddedTime = StubHelper.Date3000,
        }));

        await this.Db.BulkInsert(feedItems);

        long[] hashes =
        {
            1, 2, 3, 31, 32, 33,
        };

        var missingFeedItems = await importService.GetMissingFeedItems(1, hashes);

        Array.Sort(missingFeedItems);

        Snapshot.Match(missingFeedItems);
    }
}