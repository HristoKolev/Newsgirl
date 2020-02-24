namespace Newsgirl.WebServices.Feeds
{
    using System.Collections.Generic;
    using System.ComponentModel.DataAnnotations;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure.Api;
    using Infrastructure.Data;

    // ReSharper disable once UnusedMember.Global
    public class FeedsHandler
    {
        public FeedsHandler(FeedsService feedsService)
        {
            this.FeedsService = feedsService;
        }

        private FeedsService FeedsService { get; }

        [InTransaction]
        [BindRequest(typeof(DeleteFeedRequest), typeof(DeleteFeedResponse))]
        // ReSharper disable once UnusedMember.Global
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

        [BindRequest(typeof(NewFeedRequest), typeof(NewFeedResponse))]
        // ReSharper disable once UnusedMember.Global
        public Task<NewFeedResponse> GetNewFeed()
        {
            return Task.FromResult(new NewFeedResponse
            {
                Item = new FeedDto
                {
                    FeedName = "",
                    FeedUrl = ""
                }
            });
        }

        [BindRequest(typeof(GetFeedRequest), typeof(GetFeedResponse))]
        // ReSharper disable once UnusedMember.Global
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
                }
            });
        }

        [InTransaction]
        [BindRequest(typeof(SaveFeedRequest), typeof(SaveFeedResponse))]
        // ReSharper disable once UnusedMember.Global
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

        [BindRequest(typeof(SearchFeedsRequest), typeof(SearchFeedsResponse))]
        // ReSharper disable once UnusedMember.Global
        public async Task<SearchFeedsResponse> SearchFeeds(SearchFeedsRequest req)
        {
            var items = await this.FeedsService.GetFeeds(new FeedFM
            {
                FeedName_Contains = req.Query
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

        public int? Cats { get; set; }
    }

    public class DeleteFeedResponse
    {
        public int ID { get; set; }
    }
}