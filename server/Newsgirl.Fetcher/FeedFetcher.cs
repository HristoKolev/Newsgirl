using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Autofac;
using CodeHollow.FeedReader;
using LinqToDB;

using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;
using Npgsql;
using NpgsqlTypes;

namespace Newsgirl.Fetcher
{
    public class FeedFetcher
    {
        private readonly ILifetimeScope lifetimeScope;
        private readonly IxxHash xxHash;

        public FeedFetcher(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
            this.xxHash = xxHashFactory.Instance.Create(new xxHashConfig
            {
                HashSizeInBits = 64
            });
        }

        public async Task FetchFeeds()
        {
            List<FeedPoco> feeds;

            await using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                var db = scope.Resolve<DbService>();
                feeds = await db.Poco.Feeds.ToListAsync();
            }

            var tasks = new List<Task<FeedUpdateModel>>(feeds.Count);

            for (int i = 0; i < feeds.Count; i++)
            {
                var feed = feeds[i];
                tasks.Add(this.ProcessFeed(feed));
            }

            var updates = await Task.WhenAll(tasks);

            await using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                var db = scope.Resolve<DbService>();
                var dbConnection = scope.Resolve<NpgsqlConnection>();

                await dbConnection.OpenAsync();
                
                await using (var tx = await dbConnection.BeginTransactionAsync())
                {
                    const string header = "COPY public.feed_items (feed_id, feed_item_added_time, feed_item_description, feed_item_hash, feed_item_title, feed_item_url) FROM STDIN (FORMAT BINARY)";

                    await using (var importer = dbConnection.BeginBinaryImport(header))
                    {
                        for (int i = 0; i < updates.Length; i++)
                        {
                            var update = updates[i];

                            if (update.NewItems != null && update.NewItems.Count > 0)
                            {
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
                        }
                            
                        await importer.CompleteAsync();
                    }
                    
                    for (int i = 0; i < updates.Length; i++)
                    {
                        var update = updates[i];

                        if (update.NewItemsHash.HasValue && update.Feed != null)
                        {
                            await db.ExecuteNonQuery("update public.feeds set feed_hash = :hash where feed_id = :feed_id;",
                                db.CreateParameter("hash", update.NewItemsHash.Value),
                                db.CreateParameter("feed_id", update.Feed.FeedID)
                            );    
                        }
                    }

                    await tx.CommitAsync();
                }
            }
        }
 
        private static async Task<string> GetFeedContent(string url)
        {
            using var httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Global.SystemSettings.HttpClientRequestTimeout),
            };

            httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Global.SystemSettings.HttpClientUserAgent);
            
            var request = new HttpRequestMessage(HttpMethod.Get, url);

            using var response = await httpClient.SendAsync(request);
            
            if (!response.IsSuccessStatusCode)
            {
                throw new DetailedLogException("The feed request failed.")
                {
                    Details =
                    {
                        {"request", request},
                        {"response", response}
                    }
                };
            }
            
            try
            {
                using var content = response.Content;
                
                var responseBody = await content.ReadAsByteArrayAsync();

                return Encoding.UTF8.GetString(responseBody);
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to read the response to the feed request.", err)
                {
                    Details =
                    {
                        {"request", request},
                        {"response", response}
                    }
                };
            }
        }

        private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
        {
            try
            {
                string feedContent = await GetFeedContent(feed.FeedUrl);

                Feed materializedFeed;

                try
                {
                    materializedFeed = FeedReader.ReadFromString(feedContent);
                }
                catch (Exception err)
                {
                    throw new DetailedLogException("Failed to parse feed content.", err)
                    {
                        Details =
                        {
                            {"feed", feed},
                            {"content", feedContent}
                        }
                    };
                }

                var materializedItems = (List<FeedItem>)materializedFeed.Items;

                long combinedHash = this.GetItemsHash(materializedItems);

                if (feed.FeedHash == combinedHash)
                {
                    return new FeedUpdateModel
                    {
                        Feed = feed,
                    };
                }
           
                var itemMap = new Dictionary<long, FeedItem>();

                for (var i = 0; i < materializedItems.Count; i++)
                {
                    var materializedItem = materializedItems[i];
                    
                    long itemHash = this.GetItemHash(materializedItem);
                    
                    itemMap.Add(itemHash, materializedItem);
                }

                long[] itemHashes = itemMap.Keys.ToArray();
                
                long[] newHashes;
                
                await using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var db = scope.Resolve<DbService>();

                    newHashes = await db.ExecuteScalar<long[]>("select get_missing_feed_items(:feed_id, :hashes);",
                        db.CreateParameter("feed_id", feed.FeedID),
                        db.CreateParameter("hashes", itemHashes, NpgsqlDbType.Bigint | NpgsqlDbType.Array)
                    );
                }
                
                DateTime fetchTime = DateTime.UtcNow;
                
                var newItems = new List<FeedItemPoco>(newHashes.Length);

                for (int i = 0; i < newHashes.Length; i++)
                {
                    long itemHash = newHashes[i];
                    
                    var materializedItem = itemMap[itemHash];
                    
                    newItems.Add(new FeedItemPoco
                    {
                        FeedID = feed.FeedID,
                        FeedItemUrl = materializedItem.Link.SomethingOrNull(),
                        FeedItemTitle = materializedItem.Title.SomethingOrNull(),
                        FeedItemDescription = materializedItem.Description.SomethingOrNull(),
                        FeedItemAddedTime = fetchTime,
                        FeedItemHash = itemHash,
                    });
                }

                return new FeedUpdateModel
                {
                    Feed = feed,
                    NewItems = newItems,
                    NewItemsHash = combinedHash,
                };

            }
            catch (Exception err)
            {
                MainLogger.Error(err);
                
                return new FeedUpdateModel
                {
                    Feed = feed,
                };
            }
        }
        
        private long GetItemHash(FeedItem feedItem)
        {
            var urlBytes = Encoding.UTF8.GetBytes(feedItem.Link);

            byte[] hashedValue = this.xxHash.ComputeHash(urlBytes).Hash;
            
            long value = BitConverter.ToInt64(hashedValue);

            return value;
        }

        private long GetItemsHash(List<FeedItem> feedItems)
        {
            byte[] concatenatedUrlBytes;
            
            using (var memStream = new MemoryStream())
            {
                byte[] urlBytes;
                
                for (int i = 0; i < feedItems.Count; i++)
                {
                    urlBytes = Encoding.UTF8.GetBytes(feedItems[i].Link);
                    
                    memStream.Write(urlBytes, 0, urlBytes.Length);
                }

                concatenatedUrlBytes = memStream.ToArray();
            }

            byte[] hashedValue = this.xxHash.ComputeHash(concatenatedUrlBytes).Hash;
            
            long value = BitConverter.ToInt64(hashedValue);

            return value;
        }
    }

    public class FeedUpdateModel
    {
        public List<FeedItemPoco> NewItems { get; set; }

        public long? NewItemsHash { get; set; }
        
        public FeedPoco Feed { get; set; }
    }
}
