namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure;
    using Infrastructure.Api;
    using Infrastructure.Data;

    // ReSharper disable once UnusedMember.Global
    public class RefreshItemsHandler
    {
        public RefreshItemsHandler(
            FeedsService feedsService,
            FeedItemsClientService feedsClientService,
            IDbService db)
        {
            this.FeedsService = feedsService;
            this.FeedsClientService = feedsClientService;
            this.Db = db;
            
            this.DbLock = new AsyncLock();
        }

        private FeedsService FeedsService { get; }

        private FeedItemsClientService FeedsClientService { get; }

        private IDbService Db { get; }

        private AsyncLock DbLock { get; }

        [BindRequest(typeof(RefreshFeedsRequest))]
        // ReSharper disable once UnusedMember.Global
        public async Task<ApiResult> RefreshFeeds()
        {
            var allFeeds = await this.FeedsService.GetFeeds(new FeedFM());
            
            var tasks = allFeeds.Select(x => x.FeedID)
                                .Select(this.ProcessFeed)
                                .ToList();

            await Task.WhenAll(tasks);
     
            return ApiResult.SuccessfulResult();
        }
        
        private async Task ProcessFeed(int feedID)
        {
            try
            {
                FeedBM feed;

                using (await this.DbLock.Lock())
                {
                    feed = await this.FeedsService.Get(feedID);
                }
                    
                var items = await this.FeedsClientService.FetchFeedItems(feed.FeedUrl);
                    
                using (await this.DbLock.Lock()) 
                {
                    await this.Db.ExecuteInTransactionAndCommit(async () =>
                    {
                        await this.FeedsService.SaveItems(items, feedID);
                    });
                }
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                using (await this.DbLock.Lock())
                {
                    await this.Db.ExecuteInTransactionAndCommit(async () =>
                    {
                        var feed = await this.FeedsService.Get(feedID);
                            
                        feed.FeedLastFailedTime = DateTime.UtcNow;
                        feed.FeedLastFailedReason = exception.Message;

                        await this.FeedsService.Save(feed);
                    });
                }
            }
        }
    }
    
    public class RefreshFeedsRequest
    {
    }
}