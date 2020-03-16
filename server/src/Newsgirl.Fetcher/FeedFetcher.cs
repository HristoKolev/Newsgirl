using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using Newsgirl.Shared;
using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedFetcher
    {
        private readonly IFeedContentProvider feedContentProvider;
        private readonly IFeedParser feedParser;
        private readonly IFeedItemsImportService feedItemsImportService;
        private readonly SystemSettingsModel systemSettings;
        private readonly ITransactionService transactionService;
        private readonly Hasher hasher;
        private readonly ILog log;
        private readonly AsyncLock dbLock;

        public FeedFetcher(
            IFeedContentProvider feedContentProvider,
            IFeedParser feedParser,
            IFeedItemsImportService feedItemsImportService,
            SystemSettingsModel systemSettings,
            ITransactionService transactionService,
            Hasher hasher,
            ILog log)
        {
            this.feedContentProvider = feedContentProvider;
            this.feedParser = feedParser;
            this.feedItemsImportService = feedItemsImportService;
            this.systemSettings = systemSettings;
            this.transactionService = transactionService;
            this.hasher = hasher;
            this.log = log;
            this.dbLock = new AsyncLock();
        }

        public async Task FetchFeeds()
        {
            this.log.Log("Beginning fetch cycle...");
            
            var feeds = await this.feedItemsImportService.GetFeedsForUpdate();

            this.log.Log($"Fetching {feeds.Count} feeds.");

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
            
            this.log.Debug(() =>
            {
                int notChangedCount = updates.Count(x => x.NewItems == null || !x.NewItems.Any());

                return $"{notChangedCount} feeds unchanged.";
            });

            this.log.Debug(() =>
            {
                int changedCount = updates.Count(x => x.NewItems != null && x.NewItems.Any());

                return $"{changedCount} feeds changed.";
            });

            await this.transactionService.ExecuteInTransactionAndCommit(async () =>
            {
                await this.feedItemsImportService.ImportItems(updates);
            });

            this.log.Log("Fetch cycle complete.");
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
                    this.log.Debug($"Feed #{feed.FeedID} is not changed. Matching content hash.");
                    
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
                            {"feedContentBytes", Convert.ToBase64String(feedContentBytes)}
                        }
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
                            {"content", feedContent}
                        }
                    };
                }

                if (feed.FeedItemsHash == parsedFeed.FeedItemsHash)
                {
                    this.log.Debug($"Feed #{feed.FeedID} is not changed. Matching items hash.");
                    
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

                this.log.Debug($"Feed #{feed.FeedID} has {newHashes.Count} new items.");
                
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
                this.log.Debug($"An error occurred while fetching feed #{feed.FeedID}.");
                
                await this.log.Error(err, new Dictionary<string, object>
                {
                    {"feed", feed}
                });
                
                return new FeedUpdateModel
                {
                    Feed = feed,
                };
            }
        }
    }
}
