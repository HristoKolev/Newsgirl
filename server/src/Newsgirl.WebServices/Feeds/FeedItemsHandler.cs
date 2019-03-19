namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure;
    using Infrastructure.Api;
    using Infrastructure.Data;

    // ReSharper disable once UnusedMember.Global
    public class FeedItemsHandler
    {
        public FeedItemsHandler(
            FeedsService feedsService, 
            FeedItemsClient feedsClient,
            IDbService db)
        {    
            this.FeedsService = feedsService;
            this.FeedsClient = feedsClient;
            this.Db = db;
            
            this.DbLock = new AsyncLock();
        }

        private FeedsService FeedsService { get; }

        private FeedItemsClient FeedsClient { get; }

        private IDbService Db { get; }

        private AsyncLock DbLock { get; }

        [BindRequest(typeof(RefreshFeedsRequest))]
        // ReSharper disable once UnusedMember.Global
        public async Task<ApiResult> RefreshFeeds()
        {
            var allFeeds = await this.FeedsService.GetFeeds(new FeedFM());

            async Task ProcessFeed(int feedID)
            {
                try
                {
                    FeedBM feed;

                    using (await this.DbLock.Lock())
                    {
                        feed = await this.FeedsService.Get(feedID);
                    }
                    
                    var items = await this.FeedsClient.GetFeedItems(feed.FeedUrl);
                    
                    using (await this.DbLock.Lock()) 
                    {
                        await this.Db.ExecuteInTransactionAndCommit(async () =>
                        {
                            await this.FeedsService.SaveBulk(items, feedID);
                        });
                    }
                }
                catch (Exception ex)
                {
                    await Global.Log.LogError(ex);

                    using (await this.DbLock.Lock())
                    {
                        await this.Db.ExecuteInTransactionAndCommit(async () =>
                        {
                            var feed = await this.FeedsService.Get(feedID);
                            
                            feed.FeedLastFailedTime = DateTime.UtcNow;
                            feed.FeedLastFailedReason = ex.Message;

                            await this.FeedsService.Save(feed);
                        });
                    }
                }
            }
            
            var tasks = allFeeds.Select(x => ProcessFeed(x.FeedID)).ToList();

            await Task.WhenAll(tasks);
     
            return ApiResult.SuccessfulResult();
        }
    }
}