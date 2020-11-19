namespace Newsgirl.Fetcher
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using LinqToDB;
    using Npgsql;
    using NpgsqlTypes;
    using Shared;
    using Shared.Postgres;

    public class FeedItemsImportService : IFeedItemsImportService
    {
        private readonly IDbService db;
        private readonly NpgsqlConnection dbConnection;

        public FeedItemsImportService(IDbService db, NpgsqlConnection dbConnection)
        {
            this.db = db;
            this.dbConnection = dbConnection;
        }

        public Task<FeedPoco[]> GetFeedsForUpdate()
        {
            return this.db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToArrayAsync();
        }

        public async Task ImportItems(FeedUpdateModel[] updates)
        {
            await using (var tx = await this.db.BeginTransaction())
            {
                await using (var importer = this.dbConnection.BeginBinaryImport(this.db.GetCopyHeader<FeedItemPoco>()))
                {
                    for (int i = 0; i < updates.Length; i++)
                    {
                        var update = updates[i];

                        if (update.NewItems == null || update.NewItems.Count == 0)
                        {
                            continue;
                        }

                        for (int j = 0; j < update.NewItems.Count; j++)
                        {
                            await importer.StartRowAsync();
                            await update.NewItems[j].WriteToImporter(importer);
                        }
                    }

                    await importer.CompleteAsync();
                }

                for (int i = 0; i < updates.Length; i++)
                {
                    var update = updates[i];

                    if (update.NewFeedContentHash.HasValue && update.NewFeedItemsHash.HasValue && update.Feed != null)
                    {
                        await this.db.ExecuteNonQuery(
                            "update public.feeds set feed_items_hash = :items_hash, feed_content_hash = :content_hash where feed_id = :feed_id;",
                            this.db.CreateParameter("items_hash", update.NewFeedItemsHash.Value),
                            this.db.CreateParameter("content_hash", update.NewFeedContentHash.Value),
                            this.db.CreateParameter("feed_id", update.Feed.FeedID)
                        );
                    }
                }

                await tx.CommitAsync();
            }
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

        Task ImportItems(FeedUpdateModel[] updates);

        Task<long[]> GetMissingFeedItems(int feedID, long[] feedItemHashes);
    }

    public class FeedUpdateModel
    {
        public List<FeedItemPoco> NewItems { get; set; }

        public long? NewFeedItemsHash { get; set; }

        public long? NewFeedContentHash { get; set; }

        public FeedPoco Feed { get; set; }
    }
}
