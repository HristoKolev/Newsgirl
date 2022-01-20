namespace Newsgirl.Fetcher;

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using LinqToDB;
using NpgsqlTypes;
using Shared;

public class FeedItemsImportService : IFeedItemsImportService
{
    private readonly IDbService db;

    public FeedItemsImportService(IDbService db)
    {
        this.db = db;
    }

    public Task<FeedPoco[]> GetFeedsForUpdate()
    {
        return this.db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToArrayAsync();
    }

    public async Task ApplyUpdate(FeedUpdateModel update)
    {
        await this.db.Copy(update.NewItems);

        await this.db.ExecuteNonQuery(
            "update public.feeds set feed_items_hash = :items_hash, feed_content_hash = :content_hash where feed_id = :feed_id;",
            this.db.CreateParameter("items_hash", update.NewFeedItemsHash),
            this.db.CreateParameter("content_hash", update.NewFeedContentHash),
            this.db.CreateParameter("feed_id", update.Feed.FeedID)
        );
    }

    public Task<long[]> GetMissingFeedItems(int feedID, long[] feedItemHashes)
    {
        return this.db.ExecuteScalar<long[]>(
            "select get_missing_feed_items(:feed_id, :hashes);",
            this.db.CreateParameter("feed_id", feedID),
            this.db.CreateParameter("hashes", feedItemHashes, NpgsqlDbType.Bigint | NpgsqlDbType.Array)
        );
    }
}

public interface IFeedItemsImportService
{
    Task<FeedPoco[]> GetFeedsForUpdate();

    Task ApplyUpdate(FeedUpdateModel update);

    Task<long[]> GetMissingFeedItems(int feedID, long[] feedItemHashes);
}

public class FeedUpdateModel
{
    public List<FeedItemPoco> NewItems { get; set; }

    public long NewFeedItemsHash { get; set; }

    public long NewFeedContentHash { get; set; }

    public FeedPoco Feed { get; set; }
}