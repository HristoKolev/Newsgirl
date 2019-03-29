namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    using Infrastructure;
    using Infrastructure.Data;

    using LinqToDB;

    using PgNet;

    // ReSharper disable once ClassNeverInstantiated.Global
    public class FeedsService
    {
        public FeedsService(IDbService db)
        {
            this.Db = db;
        }

        private IDbService Db { get; }

        public Task<List<FeedCM>> GetFeeds(FeedFM filter)
        {
            return this.Db.Poco.Feeds
                       .Filter(filter)
                       .OrderByPrimaryKeyDescending()
                       .SelectCm()
                       .ToListAsync();
        }

        public async Task<FeedBM> Get(int feedID)
        {
            return (await this.Db.FindByID<FeedPoco>(feedID))?.ToBm();
        }

        public async Task<int> Save(FeedBM bm)
        {
            var poco = bm.ToPoco();

            return await this.Db.Save(poco);
        }

        public async Task Delete(int feedID)
        {
            var itemIds = await this.Db.Poco.FeedItems
                                    .Filter(new FeedItemFM { FeedID = feedID })
                                    .Select(x => x.FeedItemID)
                                    .ToArrayAsync();

            await this.Db.Delete<FeedItemPoco>(itemIds);
            await this.Db.Delete<FeedPoco>(feedID);
        }

        public async Task SaveBulk(List<FeedItemBM> items, int feedID)
        {
            var urls = items.Select(x => x.FeedItemUrl).ToList();

            var existingItems = await this.Db.Poco.FeedItems
                                     .Where(x => x.FeedID == feedID && urls.Contains(x.FeedItemUrl))
                                     .ToListAsync();

            var forUpdate = items.IntersectBy(existingItems, (bm, poco) => bm.FeedItemUrl == poco.FeedItemUrl).ToList();

            foreach (var itemBm in forUpdate)
            {
                var existingItem = existingItems.FirstOrDefault(x => x.FeedItemUrl == itemBm.FeedItemUrl);
                
                existingItem.FeedItemTitle = itemBm.FeedItemTitle;
                existingItem.FeedItemDescription = itemBm.FeedItemDescription;
                
                await this.Db.UpdateChangesOnly(existingItem);
            }
            
            var forInsert = items.ExceptBy(existingItems, (bm, poco) => bm.FeedItemUrl == poco.FeedItemUrl)
                                 .Select(x => x.ToPoco())
                                 .ToList();

            var now = DateTime.UtcNow;
            
            foreach (var itemPoco in forInsert)
            {
                itemPoco.FeedID = feedID;
                itemPoco.FeedItemAddedTime = now;

                await this.Db.Insert(itemPoco);
            }
        }
    }
}