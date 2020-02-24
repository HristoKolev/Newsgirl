namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure.Api;
    using Infrastructure.Data;

    using LinqToDB;

    // ReSharper disable once UnusedMember.Global
    public class FeedItemsHandler
    {
        private IDbService Db { get; }

        public FeedItemsHandler(IDbService db)
        {
            this.Db = db;
        }
        
        [BindRequest(typeof(GetFeedItemsRequest), typeof(GetFeedItemsResponse))]
        public async Task<GetFeedItemsResponse> GetFeeds(GetFeedItemsRequest req)
        {
            var feeds = await this.Db.Poco.Feeds.ToListAsync();
            
            var feedItems = await (from feedItem in this.Db.Poco.FeedItems
                                join feed in this.Db.Poco.Feeds on feedItem.FeedID equals feed.FeedID
                                orderby feedItem.FeedItemAddedTime descending
                                select new
                                {
                                    feedItem,
                                    feed
                                }
                               ).Take(500).ToListAsync();
            
            return new GetFeedItemsResponse
            {
                FeedItems = feedItems.Select(x => new FeedItemDto
                {
                    FeedID = x.feedItem.FeedID,
                    FeedName = x.feed.FeedName,
                    FeedItemUrl = x.feedItem.FeedItemUrl,
                    FeedItemTitle = x.feedItem.FeedItemTitle,
                    FeedItemDescription = x.feedItem.FeedItemDescription,
                    FeedItemID = x.feedItem.FeedItemID,
                    FeedItemAddedTime = x.feedItem.FeedItemAddedTime,
                }).ToList(),
                Feeds = feeds.Select(x => new FeedDto
                {
                    FeedID = x.FeedID,
                    FeedName = x.FeedName,
                    FeedUrl = x.FeedUrl,
                }).ToList()
            };
        }
    }

    public class GetFeedItemsRequest
    {
    }

    public class GetFeedItemsResponse
    {
        public List<FeedItemDto> FeedItems { get; set; }

        public List<FeedDto> Feeds { get; set; }
    }

    public class FeedItemDto
    {
        public string FeedName { get; set; }
        
        public int FeedID { get; set; }

        public DateTime FeedItemAddedTime { get; set; }

        public string FeedItemDescription { get; set; }

        public int FeedItemID { get; set; }

        public string FeedItemTitle { get; set; }

        public string FeedItemUrl { get; set; }
    }
}