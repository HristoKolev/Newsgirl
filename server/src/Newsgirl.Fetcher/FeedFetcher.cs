namespace Newsgirl.Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Shared;
    using Shared.Logging;

    public class FeedFetcher
    {
        private readonly IFeedContentProvider feedContentProvider;
        private readonly IFeedParser feedParser;
        private readonly IFeedItemsImportService feedItemsImportService;
        private readonly SystemSettingsModel systemSettings;
        private readonly DbTransactionService transactionService;
        private readonly Hasher hasher;
        private readonly ILog log;
        private readonly ErrorReporter errorReporter;
        private readonly AsyncLock dbLock;

        public FeedFetcher(
            IFeedContentProvider feedContentProvider,
            IFeedParser feedParser,
            IFeedItemsImportService feedItemsImportService,
            SystemSettingsModel systemSettings,
            DbTransactionService transactionService,
            Hasher hasher,
            ILog log,
            ErrorReporter errorReporter)
        {
            this.feedContentProvider = feedContentProvider;
            this.feedParser = feedParser;
            this.feedItemsImportService = feedItemsImportService;
            this.systemSettings = systemSettings;
            this.transactionService = transactionService;
            this.hasher = hasher;
            this.log = log;
            this.errorReporter = errorReporter;
            this.dbLock = new AsyncLock();
        }

        public async Task FetchFeeds()
        {
            this.log.General(() => new LogData("Beginning fetch cycle..."));

            var feeds = await this.feedItemsImportService.GetFeedsForUpdate();

            this.log.General(() => new LogData("Feeds ready for update.")
            {
                {"feedCount", feeds.Count},
            });

            FeedUpdateModel[] updates;

            if (this.systemSettings.ParallelFeedFetching)
            {
                var tasks = new List<Task<FeedUpdateModel>>(feeds.Count);

                for (int i = 0; i < feeds.Count; i++)
                {
                    var feed = feeds[i];
                    tasks.Add(this.ProcessFeed(feed));
                }

                updates = await Task.WhenAll(tasks);
            }
            else
            {
                updates = new FeedUpdateModel[feeds.Count];

                for (int i = 0; i < feeds.Count; i++)
                {
                    var feed = feeds[i];
                    updates[i] = await this.ProcessFeed(feed);
                }
            }

            this.log.General(() =>
            {
                int changedCount = updates.Count(update => update.NewItems != null && update.NewItems.Any());
                int unchangedCount = updates.Count(update => update.NewItems == null || !update.NewItems.Any());

                return new LogData("Updates ready for import.")
                {
                    {"changedCount", changedCount},
                    {"unchangedCount", unchangedCount},
                };
            });

            await this.transactionService.ExecuteInTransactionAndCommit(async () =>
            {
                await this.feedItemsImportService.ImportItems(updates);
            });

            this.log.General(() => new LogData("Fetch cycle complete."));
        }

        private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
        {
            try
            {
                byte[] feedContentBytes;

                try
                {
                    feedContentBytes = await this.feedContentProvider.GetFeedContent(feed);
                }
                catch (Exception err)
                {
                    throw new DetailedLogException("The http request for the feed failed.", err)
                    {
                        Fingerprint = "FEED_HTTP_REQUEST_FAILED",
                    };
                }

                long feedContentHash = this.hasher.ComputeHash(feedContentBytes);

                if (feedContentHash == feed.FeedContentHash)
                {
                    this.log.General(() => new LogData("Feed not changed. Matching content hash.")
                    {
                        {"feedID", feed.FeedID},
                    });

                    return new FeedUpdateModel
                    {
                        Feed = feed,
                    };
                }

                string feedContent;

                try
                {
                    feedContent = EncodingHelper.UTF8.GetString(feedContentBytes);
                }
                catch (Exception err)
                {
                    throw new DetailedLogException("Failed to parse UTF-8 feed content.", err)
                    {
                        Fingerprint = "FEED_CONTENT_UTF8_PARSE_FAILED",
                        Details =
                        {
                            {"feedContentBytes", Convert.ToBase64String(feedContentBytes)},
                        },
                    };
                }

                ParsedFeed parsedFeed;

                try
                {
                    parsedFeed = this.feedParser.Parse(feedContent);
                }
                catch (Exception err)
                {
                    throw new DetailedLogException("Failed to parse feed content.", err)
                    {
                        Fingerprint = "FEED_CONTENT_PARSE_FAILED",
                        Details =
                        {
                            {"content", feedContent},
                        },
                    };
                }

                if (feed.FeedItemsHash == parsedFeed.FeedItemsHash)
                {
                    this.log.General(() => new LogData("Feed not changed. Matching items hash.")
                    {
                        {"feedID", feed.FeedID},
                    });

                    return new FeedUpdateModel
                    {
                        Feed = feed,
                    };
                }

                long[] itemHashes = parsedFeed.FeedItemHashes.ToArray();

                HashSet<long> newHashes;

                using (await this.dbLock.Lock())
                {
                    var newHashArray = await this.feedItemsImportService.GetMissingFeedItems(feed.FeedID, itemHashes);

                    newHashes = new HashSet<long>(newHashArray);
                }

                this.log.General(() => new LogData("Feed changed.")
                {
                    {"feedID", feed.FeedID},
                    {"updateCount", newHashes.Count},
                });

                var newItems = new List<FeedItemPoco>(newHashes.Count);

                for (int i = 0; i < parsedFeed.Items.Count; i++)
                {
                    var feedItem = parsedFeed.Items[i];

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
                    NewFeedItemsHash = parsedFeed.FeedItemsHash,
                    NewFeedContentHash = feedContentHash,
                };
            }
            catch (Exception err)
            {
                this.log.General(() => new LogData("An error occurred while fetching feed.")
                {
                    {"feedID", feed.FeedID},
                });

                await this.errorReporter.Error(err, new Dictionary<string, object>
                {
                    {"feed", feed},
                });

                return new FeedUpdateModel
                {
                    Feed = feed,
                };
            }
        }
    }
}
