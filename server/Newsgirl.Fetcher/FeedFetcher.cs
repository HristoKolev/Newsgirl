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
        private readonly AsyncLock dbLock;

        public FeedFetcher(
            IFeedContentProvider feedContentProvider,
            IFeedParser feedParser,
            IFeedItemsImportService feedItemsImportService,
            SystemSettingsModel systemSettings,
            ITransactionService transactionService)
        {
            this.feedContentProvider = feedContentProvider;
            this.feedParser = feedParser;
            this.feedItemsImportService = feedItemsImportService;
            this.systemSettings = systemSettings;
            this.transactionService = transactionService;
            this.dbLock = new AsyncLock();
        }

        public async Task FetchFeeds()
        {
            MainLogger.Print("Beginning fetch cycle...");
            
            var feeds = await this.feedItemsImportService.GetFeedsForUpdate();
            
            MainLogger.Print($"Fetching {feeds.Count} feeds.");

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

            if (Global.Debug)
            {
                int notChangedCount = updates.Count(x => x.NewItems == null || !x.NewItems.Any());
                
                MainLogger.Debug($"{notChangedCount} feeds unchanged.");
                
                int changedCount = updates.Count(x => x.NewItems != null && x.NewItems.Any());
                
                MainLogger.Debug($"{changedCount} feeds changed.");
            }

            await this.transactionService.ExecuteInTransactionAndCommit(async () =>
            {
                await this.feedItemsImportService.ImportItems(updates);
            });

            MainLogger.Print("Fetch cycle complete in.");
        }

        private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
        {
            try
            {
                string feedContent;
                
                try
                {
                    feedContent = await this.feedContentProvider.GetFeedContent(feed);
                }
                catch (Exception err)
                {
                    throw new DetailedLogException("The http request for the feed failed.", err)
                    {
                        Fingerprint = "FEED_HTTP_REQUEST_FAILED",
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

                if (feed.FeedHash == parsedFeed.FeedHash)
                {
                    MainLogger.Debug($"Feed #{feed.FeedID} is not changed. Matching combined hash.");
                    
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

                MainLogger.Debug($"Feed #{feed.FeedID} has {newHashes.Count} new items.");
                
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
                    NewFeedHash = parsedFeed.FeedHash,
                };
            }
            catch (Exception err)
            {
                MainLogger.Debug($"An error occurred while fetching feed #{feed.FeedID}.");
                
                MainLogger.Error(err, new Dictionary<string, object>
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
