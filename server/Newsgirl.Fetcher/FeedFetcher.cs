namespace Newsgirl.Fetcher;

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shared;
using Xdxd.DotNet.Shared;

public class FeedFetcher
{
    private readonly IFeedContentProvider feedContentProvider;
    private readonly IFeedParser feedParser;
    private readonly IFeedItemsImportService feedItemsImportService;
    private readonly DateTimeService dateTimeService;
    private readonly ErrorReporter errorReporter;
    private readonly IDbService dbService;
    private readonly AsyncLock dbLock;

    public FeedFetcher(
        IFeedContentProvider feedContentProvider,
        IFeedParser feedParser,
        IFeedItemsImportService feedItemsImportService,
        DateTimeService dateTimeService,
        ErrorReporter errorReporter,
        IDbService dbService)
    {
        this.feedContentProvider = feedContentProvider;
        this.feedParser = feedParser;
        this.feedItemsImportService = feedItemsImportService;
        this.dateTimeService = dateTimeService;
        this.errorReporter = errorReporter;
        this.dbService = dbService;
        this.dbLock = new AsyncLock();
    }

    public async Task<FetcherRunData> FetchFeeds()
    {
        var fetcherRunData = new FetcherRunData
        {
            StartTime = this.dateTimeService.EventTime(),
        };

        var feeds = await this.feedItemsImportService.GetFeedsForUpdate();

        fetcherRunData.FeedCount = feeds.Length;

        var updates = (await Task.WhenAll(feeds.Select(this.ProcessFeed))).Where(x => x != null).ToArray();

        fetcherRunData.ChangedFeedCount = updates.Length;
        fetcherRunData.ChangedFeedItemCount = updates.SelectMany(x => x.NewItems).Count();
        fetcherRunData.EndTime = this.dateTimeService.CurrentTime();
        fetcherRunData.Duration = (long)(fetcherRunData.EndTime - fetcherRunData.StartTime).TotalMilliseconds;

        return fetcherRunData;
    }

    private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
    {
        byte[] feedContent = null;
        long feedContentHash = 0;
        string feedContentString = null;
        ParsedFeed parsedFeed = null;

        try
        {
            // Get the bytes from the network.
            try
            {
                feedContent = await this.feedContentProvider.GetFeedContent(feed);
            }
            catch (Exception ex)
            {
                throw new DetailedException("The http request for the feed failed.", ex)
                {
                    Fingerprint = "FEED_HTTP_REQUEST_FAILED",
                };
            }

            feedContentHash = HashHelper.ComputeXx64Hash(feedContent);

            // The bytes have not changed. 
            if (feedContentHash == feed.FeedContentHash)
            {
                return null;
            }

            // Parse into a string.
            try
            {
                feedContentString = EncodingHelper.UTF8.GetString(feedContent);
            }
            catch (Exception ex)
            {
                throw new DetailedException("Failed to parse UTF-8 feed content.", ex)
                {
                    Fingerprint = "FEED_CONTENT_UTF8_PARSE_FAILED",
                };
            }

            // Parse the RSS.
            try
            {
                parsedFeed = this.feedParser.Parse(feedContentString, feed.FeedID);
            }
            catch (Exception err)
            {
                throw new DetailedException("Failed to parse feed content.", err)
                {
                    Fingerprint = "FEED_CONTENT_PARSE_FAILED",
                };
            }

            // The information that we care about has not changed.
            if (feed.FeedItemsHash == parsedFeed.FeedItemsHash)
            {
                return null;
            }

            // Get the hashes of items that don't appear in the database.
            long[] itemHashes = parsedFeed.FeedItemHashes.ToArray();

            long[] newHashArray;
            using (await this.dbLock.Lock())
            {
                newHashArray = await this.feedItemsImportService.GetMissingFeedItems(feed.FeedID, itemHashes);
            }

            var newHashes = new HashSet<long>(newHashArray);

            // Get the items that don't appear in the database.
            var newItems = new List<FeedItemPoco>(newHashes.Count);

            foreach (var feedItem in parsedFeed.Items)
            {
                if (!newHashes.Contains(feedItem.FeedItemStringIDHash))
                {
                    continue;
                }

                feedItem.FeedID = feed.FeedID;
                newItems.Add(feedItem);
            }

            var update = new FeedUpdateModel
            {
                Feed = feed,
                NewItems = newItems,
                NewFeedItemsHash = parsedFeed.FeedItemsHash,
                NewFeedContentHash = feedContentHash,
            };

            using (await this.dbLock.Lock())
            {
                await using (var tx = await this.dbService.BeginTransaction())
                {
                    await this.feedItemsImportService.ApplyUpdate(update);

                    await tx.CommitAsync();
                }
            }

            return update;
        }
        catch (Exception err)
        {
            await this.errorReporter.Error(err, new Dictionary<string, object>
            {
                { "feed", feed },
                { "feedContent", feedContent == null ? null : Convert.ToBase64String(feedContent, 0, feedContent.Length) },
                { "feedContentHash", feedContentHash },
                { "feedContentString", feedContentString },
                { "parsedFeed", parsedFeed },
            });

            return null;
        }
    }
}