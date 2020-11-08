namespace Newsgirl.Fetcher
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using LinqToDB;
    using Npgsql;
    using NpgsqlTypes;
    using Shared;
    using Shared.Logging;
    using Shared.Postgres;

    public class FeedItemsImportService : IFeedItemsImportService
    {
        private readonly IDbService db;
        private readonly NpgsqlConnection dbConnection;
        private readonly Log log;

        public FeedItemsImportService(IDbService db, NpgsqlConnection dbConnection, Log log)
        {
            this.db = db;
            this.dbConnection = dbConnection;
            this.log = log;
        }

        public Task<List<FeedPoco>> GetFeedsForUpdate()
        {
            return this.db.Poco.Feeds.OrderByDescending(x => x.FeedID).ToListAsync();
        }

        public async Task ImportItems(FeedUpdateModel[] updates)
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
                        var newItem = update.NewItems[j];

                        await importer.StartRowAsync();

                        await newItem.WriteToImporter(importer);
                    }
                }

                await importer.CompleteAsync();
            }

            this.log.General(() =>
            {
                int importedCount = updates.Select(u => u.NewItems?.Count ?? 0).Sum();

                return new LogData("Feed items imported.")
                {
                    {"importedCount", importedCount},
                };
            });

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

            this.log.General(() => new LogData("Feed hashes updated.")
            {
                {"updateCount", updates.Length},
            });
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
        Task<List<FeedPoco>> GetFeedsForUpdate();

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
