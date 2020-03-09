using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

using LinqToDB;
using Npgsql;
using NpgsqlTypes;

using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedItemsImportService : IFeedItemsImportService
    {
        private readonly DbService db;
        private readonly NpgsqlConnection dbConnection;
        private readonly ILog log;

        public FeedItemsImportService(DbService db, NpgsqlConnection dbConnection, ILog log)
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
            this.log.Debug("Importing feed items...");
                
            const string header =
                "COPY public.feed_items " +
                "(feed_id, feed_item_added_time, feed_item_description, feed_item_hash, feed_item_title, feed_item_url) " +
                "FROM STDIN (FORMAT BINARY)";

            await using (var importer = this.dbConnection.BeginBinaryImport(header))
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

                        await importer.WriteAsync(newItem.FeedID, NpgsqlDbType.Integer);

                        await importer.WriteAsync(newItem.FeedItemAddedTime, NpgsqlDbType.Timestamp);

                        if (newItem.FeedItemDescription == null)
                        {
                            await importer.WriteNullAsync();
                        }
                        else
                        {
                            await importer.WriteAsync(newItem.FeedItemDescription, NpgsqlDbType.Text);
                        }

                        await importer.WriteAsync(newItem.FeedItemHash, NpgsqlDbType.Bigint);

                        if (newItem.FeedItemTitle == null)
                        {
                            await importer.WriteNullAsync();
                        }
                        else
                        {
                            await importer.WriteAsync(newItem.FeedItemTitle, NpgsqlDbType.Text);
                        }

                        if (newItem.FeedItemUrl == null)
                        {
                            await importer.WriteNullAsync();
                        }
                        else
                        {
                            await importer.WriteAsync(newItem.FeedItemUrl, NpgsqlDbType.Text);
                        }
                    }
                }

                await importer.CompleteAsync();
            }

            this.log.Debug("Updating the feeds hashes...");

            for (int i = 0; i < updates.Length; i++)
            {
                var update = updates[i];

                if (update.NewFeedHash.HasValue && update.Feed != null)
                {
                    await this.db.ExecuteNonQuery(
                        "update public.feeds set feed_hash = :hash where feed_id = :feed_id;",
                        this.db.CreateParameter("hash", update.NewFeedHash.Value),
                        this.db.CreateParameter("feed_id", update.Feed.FeedID)
                    );
                }
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
        Task<List<FeedPoco>> GetFeedsForUpdate();

        Task ImportItems(FeedUpdateModel[] updates);

        Task<long[]> GetMissingFeedItems(int feedID, long[] feedItemHashes);
    }
    
    public class FeedUpdateModel
    {
        public List<FeedItemPoco> NewItems { get; set; }

        public long? NewFeedHash { get; set; }
        
        public FeedPoco Feed { get; set; }
    }
}
