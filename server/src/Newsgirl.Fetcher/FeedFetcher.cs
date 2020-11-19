namespace Newsgirl.Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Shared;

    public class FeedFetcher
    {
        private readonly IFeedContentProvider feedContentProvider;
        private readonly IFeedParser feedParser;
        private readonly IFeedItemsImportService feedItemsImportService;
        private readonly SystemSettingsModel systemSettings;
        private readonly DateTimeService dateTimeService;
        private readonly ErrorReporter errorReporter;
        private readonly AsyncLock dbLock;

        public FeedFetcher(
            IFeedContentProvider feedContentProvider,
            IFeedParser feedParser,
            IFeedItemsImportService feedItemsImportService,
            SystemSettingsModel systemSettings,
            DateTimeService dateTimeService,
            ErrorReporter errorReporter)
        {
            this.feedContentProvider = feedContentProvider;
            this.feedParser = feedParser;
            this.feedItemsImportService = feedItemsImportService;
            this.systemSettings = systemSettings;
            this.dateTimeService = dateTimeService;
            this.errorReporter = errorReporter;
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

            FeedUpdateModel[] updates;

            if (this.systemSettings.ParallelFeedFetching)
            {
                var tasks = new Task<FeedUpdateModel>[feeds.Length];

                for (int i = 0; i < feeds.Length; i++)
                {
                    tasks[i] = this.ProcessFeed(feeds[i]);
                }

                updates = await Task.WhenAll(tasks);
            }
            else
            {
                updates = new FeedUpdateModel[feeds.Length];

                for (int i = 0; i < feeds.Length; i++)
                {
                    updates[i] = await this.ProcessFeed(feeds[i]);
                }
            }

            await this.feedItemsImportService.ImportItems(updates);

            (int feedCount, int feedItemCount) = GetChangedFeedCount(updates);

            fetcherRunData.ChangedFeedCount = feedCount;
            fetcherRunData.ChangedFeedItemCount = feedItemCount;

            fetcherRunData.EndTime = this.dateTimeService.CurrentTime();
            fetcherRunData.Duration = (long) (fetcherRunData.EndTime - fetcherRunData.StartTime).TotalMilliseconds;

            return fetcherRunData;
        }

        private static (int, int) GetChangedFeedCount(FeedUpdateModel[] updates)
        {
            int feedCount = 0;
            int feedItemCount = 0;

            for (int i = 0; i < updates.Length; i++)
            {
                var items = updates[i].NewItems;

                if (items != null && items.Count != 0)
                {
                    feedCount += 1;
                    feedItemCount += items.Count;
                }
            }

            return (feedCount, feedItemCount);
        }

        private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
        {
            var state = new FeedProcessingState
            {
                Feed = feed,
            };

            try
            {
                // Get the bytes from the network.
                try
                {
                    state.FeedContent = await this.feedContentProvider.GetFeedContent(feed);
                }
                catch (Exception ex)
                {
                    throw new DetailedException("The http request for the feed failed.", ex)
                    {
                        Fingerprint = "FEED_HTTP_REQUEST_FAILED",
                        Details = {{"feedProcessingState", state}},
                    };
                }

                state.FeedContentHash = HashHelper.ComputeXx64Hash(state.FeedContent);

                // The bytes have not changed. 
                if (state.FeedContentHash == feed.FeedContentHash)
                {
                    return new FeedUpdateModel {Feed = feed};
                }

                // Parse into a string.
                try
                {
                    state.FeedContentString = EncodingHelper.UTF8.GetString(state.FeedContent);
                }
                catch (Exception ex)
                {
                    throw new DetailedException("Failed to parse UTF-8 feed content.", ex)
                    {
                        Fingerprint = "FEED_CONTENT_UTF8_PARSE_FAILED",
                        Details = {{"feedProcessingState", state}},
                    };
                }

                // Parse the RSS.
                try
                {
                    state.ParsedFeed = this.feedParser.Parse(state.FeedContentString);
                }
                catch (Exception err)
                {
                    throw new DetailedException("Failed to parse feed content.", err)
                    {
                        Fingerprint = "FEED_CONTENT_PARSE_FAILED",
                        Details = {{"feedProcessingState", state}},
                    };
                }

                // The information that we care about has not changed.
                if (feed.FeedItemsHash == state.ParsedFeed.FeedItemsHash)
                {
                    return new FeedUpdateModel {Feed = feed};
                }

                // Get the hashes of items that don't appear in the database.
                HashSet<long> newHashes;
                long[] itemHashes = state.ParsedFeed.FeedItemHashes.ToArray();
                using (await this.dbLock.Lock())
                {
                    var newHashArray = await this.feedItemsImportService.GetMissingFeedItems(feed.FeedID, itemHashes);
                    newHashes = new HashSet<long>(newHashArray);
                }

                // Get the items that don't appear in the database.
                var newItems = new List<FeedItemPoco>(newHashes.Count);

                for (int i = 0; i < state.ParsedFeed.Items.Count; i++)
                {
                    var feedItem = state.ParsedFeed.Items[i];
                    if (!newHashes.Contains(feedItem.FeedItemHash))
                    {
                        continue;
                    }

                    feedItem.FeedID = feed.FeedID;
                    newItems.Add(feedItem);
                }

                return new FeedUpdateModel
                {
                    Feed = feed,
                    NewItems = newItems,
                    NewFeedItemsHash = state.ParsedFeed.FeedItemsHash,
                    NewFeedContentHash = state.FeedContentHash,
                };
            }
            catch (Exception err)
            {
                await this.errorReporter.Error(err, new Dictionary<string, object> {{"feedProcessingState", state}});
                return new FeedUpdateModel {Feed = feed};
            }
        }

        private class FeedProcessingState
        {
            public FeedPoco Feed { get; set; }

            public byte[] FeedContent { get; set; }

            public long FeedContentHash { get; set; }

            public string FeedContentString { get; set; }

            public ParsedFeed ParsedFeed { get; set; }
        }
    }
}
