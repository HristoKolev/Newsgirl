namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure;
    using Infrastructure.Data;

    public class FeedsHandler
    {
        private FeedsService FeedsService { get; }

        private FeedItemsClient FeedsClient { get; }

        private IDbService Db { get; }

        private AsyncLock DbLock { get; }

        public FeedsHandler(
            FeedsService feedsService, 
            FeedItemsClient feedsClient,
            IDbService db)
        {    
            this.FeedsService = feedsService;
            this.FeedsClient = feedsClient;
            this.Db = db;
            
            this.DbLock = new AsyncLock();
        }
        
        [BindRequest(typeof(RefreshFeedsRequest))]
        public async Task<ApiResult> RefreshFeeds()
        {
            var allFeeds = await this.FeedsService.GetFeeds(new FeedFM());

            async Task processFeed(int feedID)
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
            
            var tasks = allFeeds.Select(x => processFeed(x.FeedID)).ToList();

            await Task.WhenAll(tasks);
     
            return ApiResult.SuccessfulResult();
        }

        [BindRequest(typeof(DeleteFeedRequest)), InTransaction]
        public async Task<ApiResult> DeleteFeed(DeleteFeedRequest req)
        {
            var poco = await this.FeedsService.Get(req.ID);

            if (poco == null)
            {
                return ApiResult.FromErrorMessage($"No feeds with ID: {req.ID}.");
            }

            await this.FeedsService.Delete(req.ID);

            return ApiResult.SuccessfulResult(new DeleteFeedResponse
            {
                ID = req.ID
            });
        }

        [BindRequest(typeof(NewFeedRequest))]
        public Task<NewFeedResponse> GetNewFeed()
        {
            return Task.FromResult(new NewFeedResponse
            {
                Item = new FeedDto
                {
                    FeedName = "",
                    FeedUrl = "",
                },
            });
        }
 
        [BindRequest(typeof(GetFeedRequest))]
        public async Task<ApiResult> GetFeed(GetFeedRequest req)
        {
            var bm = await this.FeedsService.Get(req.ID);

            if (bm == null)
            {
                return ApiResult.FromErrorMessage($"No feeds with ID: {req.ID}.");
            }

            return ApiResult.SuccessfulResult(new GetFeedResponse
            {
                Item = new FeedDto
                {
                    FeedID = bm.FeedID,
                    FeedName = bm.FeedName,
                    FeedUrl = bm.FeedUrl
                },
            });
        }

        [BindRequest(typeof(SaveFeedRequest)), InTransaction]
        public async Task<SaveFeedResponse> SaveFeed(SaveFeedRequest req)
        {
            var bm = new FeedBM
            {
                FeedID = req.Item.FeedID,
                FeedName = req.Item.FeedName,
                FeedUrl = req.Item.FeedUrl
            };

            int id = await this.FeedsService.Save(bm);

            return new SaveFeedResponse
            {
                ID = id
            };
        }

        [BindRequest(typeof(SearchFeedsRequest))]
        public async Task<SearchFeedsResponse> SearchFeeds(SearchFeedsRequest req)
        {
            var items = await this.FeedsService.GetFeeds(new FeedFM
            {
                FeedName_Contains = req.Query,
            });
            
            return new SearchFeedsResponse
            {
                Items = items.Select(x => new FeedDto
                {
                    FeedID = x.FeedID, 
                    FeedName = x.FeedName, 
                    FeedUrl = x.FeedUrl
                }).ToList()
            };
        }
    }

    public class RefreshFeedsRequest
    {
    }

    public class FeedDto
    {
        public int FeedID { get; set; }
        
        [Required(ErrorMessage = "Please, enter a feed name.")]
        public string FeedName { get; set; }
        
        [Required(ErrorMessage = "Please, enter a feed url.")]
        public string FeedUrl { get; set; }
    }
    
    public class NewFeedRequest
    {
    }

    public class NewFeedResponse
    {
        public FeedDto Item { get; set; }
    }

    public class GetFeedRequest
    {
        public int ID { get; set; }
    }

    public class GetFeedResponse
    {
        public FeedDto Item { get; set; }
    }

    public class SearchFeedsRequest
    {
        public string Query { get; set; }
    }

    public class SearchFeedsResponse
    {
        public List<FeedDto> Items { get; set; }
    }

    public class SaveFeedRequest
    {
        public FeedDto Item { get; set; }
    }

    public class SaveFeedResponse
    {
        public int ID { get; set; }
    }

    public class DeleteFeedRequest
    {
        public int ID { get; set; }
    }

    public class DeleteFeedResponse
    {
        public int ID { get; set; }
    }
}