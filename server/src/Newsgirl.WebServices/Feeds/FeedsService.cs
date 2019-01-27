namespace Newsgirl.WebServices.Feeds
{
    using System.Collections.Generic;
    using System.Threading.Tasks;

    using Infrastructure.Data;

    using LinqToDB;

    using PgNet;

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
            await this.Db.Delete<FeedPoco>(feedID);
        }

        public async Task SaveBulk()
        {
            
        }
    }
}