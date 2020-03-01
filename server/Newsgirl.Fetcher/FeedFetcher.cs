using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using Autofac;
using CodeHollow.FeedReader;
using LinqToDB;
using Newtonsoft.Json;
using Npgsql;
using NpgsqlTypes;

using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedFetcher
    {
        private readonly ILifetimeScope lifetimeScope;
        private readonly IxxHash xxHash;
        private readonly HttpClient httpClient;
        
        public FeedFetcher(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
            this.xxHash = xxHashFactory.Instance.Create(new xxHashConfig
            {
                HashSizeInBits = 64
            });

            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(Global.SystemSettings.HttpClientRequestTimeout),
            };

            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(Global.SystemSettings.HttpClientUserAgent);
        }

        public async Task FetchFeeds()
        {
            MainLogger.Print("Beginning fetch cycle...");
            var sw = Stopwatch.StartNew();
            
            // Get the feeds scheduled for checking.
            List<FeedPoco> feeds;
            await using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                var db = scope.Resolve<DbService>();
                feeds = await db.Poco.Feeds.ToListAsync();
            }
            
            MainLogger.Print($"Fetching {feeds.Count} feeds.");

            // Run the fetch tasks.
            var tasks = new List<Task<FeedUpdateModel>>(feeds.Count);
            for (int i = 0; i < feeds.Count; i++)
            {
                var feed = feeds[i];
                tasks.Add(this.ProcessFeed(feed));
            }

            var updates = await Task.WhenAll(tasks);

            if (Global.Debug)
            {
                int notChangedCount = updates.Count(x => x.NewItems == null || !x.NewItems.Any());
                
                MainLogger.Debug($"{notChangedCount} feeds unchanged.");
                
                int changedCount = updates.Count(x => x.NewItems != null && x.NewItems.Any());
                
                MainLogger.Debug($"{changedCount} feeds changed.");
            }

            // Save the data to db.
            await using (var scope = this.lifetimeScope.BeginLifetimeScope())
            {
                var db = scope.Resolve<DbService>();
                var dbConnection = scope.Resolve<NpgsqlConnection>();

                await dbConnection.OpenAsync();
                
                await using (var tx = await dbConnection.BeginTransactionAsync())
                {
                    MainLogger.Debug("Importing items...");
                    
                    const string header = "COPY public.feed_items (feed_id, feed_item_added_time, feed_item_description, feed_item_hash, feed_item_title, feed_item_url) FROM STDIN (FORMAT BINARY)";

                    await using (var importer = dbConnection.BeginBinaryImport(header))
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
                    
                    MainLogger.Debug("Updating the feeds hashes...");
                    
                    for (int i = 0; i < updates.Length; i++)
                    {
                        var update = updates[i];

                        if (update.NewFeedHash.HasValue && update.Feed != null)
                        {
                            await db.ExecuteNonQuery("update public.feeds set feed_hash = :hash where feed_id = :feed_id;",
                                db.CreateParameter("hash", update.NewFeedHash.Value),
                                db.CreateParameter("feed_id", update.Feed.FeedID)
                            );    
                        }
                    }

                    await tx.CommitAsync();
                }
            }
            
            sw.Stop();
            
            MainLogger.Print($"Fetch cycle complete in {sw.Elapsed}.");
        }
 
        private async Task<FeedUpdateModel> ProcessFeed(FeedPoco feed)
        {
            try
            {
                string feedContent = await this.GetFeedContent(feed);

                var parsedFeed = this.ParseFeed(feed, feedContent);

                if (feed.FeedHash == parsedFeed.FeedHash)
                {
                    MainLogger.Debug($"Feed #{feed.FeedID} is not changed. Matching combined hash.");
                    
                    return new FeedUpdateModel
                    {
                        Feed = feed,
                    };
                }

                long[] itemHashes = parsedFeed.FeedItemHashes.ToArray();
                
                HashSet<long> newHashes;
                
                await using (var scope = this.lifetimeScope.BeginLifetimeScope())
                {
                    var db = scope.Resolve<DbService>();

                    var newHashArray = await db.ExecuteScalar<long[]>("select get_missing_feed_items(:feed_id, :hashes);",
                        db.CreateParameter("feed_id", feed.FeedID),
                        db.CreateParameter("hashes", itemHashes, NpgsqlDbType.Bigint | NpgsqlDbType.Array)
                    );
                    
                    newHashes = new HashSet<long>(newHashArray);
                }
                
                MainLogger.Debug($"Feed #{feed.FeedID} has {newHashes.Count} new items.");
                
                DateTime fetchTime = DateTime.UtcNow;
                
                var newItems = new List<FeedItemPoco>(newHashes.Count);

                for (int i = 0; i < parsedFeed.Items.Count; i++)
                {
                    var parsedFeedItem = parsedFeed.Items[i];

                    if (!newHashes.Contains(parsedFeedItem.FeedItemHash))
                    {
                        continue;
                    }

                    var item = parsedFeedItem.Item;

                    newItems.Add(new FeedItemPoco
                    {
                        FeedID = feed.FeedID,
                        FeedItemUrl = GetItemUrl(item).SomethingOrNull()?.Trim(),
                        FeedItemTitle = item.Title.SomethingOrNull()?.Trim(),
                        FeedItemDescription = item.Description.SomethingOrNull()?.Trim(),
                        FeedItemAddedTime = fetchTime,
                        FeedItemHash = parsedFeedItem.FeedItemHash,
                    });
                }

                return new FeedUpdateModel
                {
                    Feed = feed,
                    NewItems = newItems,
                    NewFeedHash = parsedFeed.FeedHash,
                };
            }
            catch (Exception err)
            {
                MainLogger.Debug($"An error occurred while fetching feed #{feed.FeedID}.");
                
                MainLogger.Error(err, new Dictionary<string, object>
                {
                    {"feed", feed}
                });
                
                return new FeedUpdateModel
                {
                    Feed = feed,
                };
            }
        }

        private ParsedFeed ParseFeed(FeedPoco feed, string feedContent)
        {
            try
            {
                var materializedFeed = FeedReader.ReadFromString(feedContent);
                
                var allItems = (List<FeedItem>)materializedFeed.Items;

                var parsedFeed = new ParsedFeed(allItems.Count);

                using (var memStream = new MemoryStream(allItems.Count * 8))
                {
                    byte[] stringIDBytes;

                    for (int i = allItems.Count - 1; i >= 0; i--)
                    {
                        var item = allItems[i];
                
                        string stringID = GetItemStringID(item);

                        if (stringID == null)
                        {
                            if (Global.Debug)
                            {
                                MainLogger.Debug($"Cannot ID feed item: {JsonConvert.SerializeObject(item)}");
                            }
                        
                            continue;
                        }
                
                        stringIDBytes = Encoding.UTF8.GetBytes(stringID);
                    
                        long itemHash = BitConverter.ToInt64(this.xxHash.ComputeHash(stringIDBytes).Hash);

                        if (!parsedFeed.FeedItemHashes.Add(itemHash))
                        {
                            if (Global.Debug)
                            {
                                MainLogger.Debug($"Feed item already added: {stringID}");
                            }
                        
                            continue;
                        }

                        parsedFeed.Items.Add(new ParsedFeedItem
                        {
                            Item = item,
                            FeedItemHash = itemHash,
                        });
                    
                        memStream.Write(stringIDBytes, 0, stringIDBytes.Length);
                    }
                
                    var combinedStringID = memStream.ToArray();
                    var feedHashBytes = this.xxHash.ComputeHash(combinedStringID).Hash;
                
                    parsedFeed.FeedHash = BitConverter.ToInt64(feedHashBytes);
                }

                return parsedFeed;
            }
            catch (Exception err)
            {
                throw new DetailedLogException("Failed to parse feed content.", err)
                {
                    Fingerprint = "FEED_CONTENT_PARSE_FAILED",
                    Details =
                    {
                        {"feed", feed},
                        {"content", feedContent}
                    }
                };
            }
        }

        private async Task<string> GetFeedContent(FeedPoco feed)
        {
            try
            {
                using (var response = await this.httpClient.GetAsync(feed.FeedUrl))
                {
                    response.EnsureSuccessStatusCode();

                    using (var content = response.Content)
                    {
                        var responseBody = await content.ReadAsByteArrayAsync();

                        return Encoding.UTF8.GetString(responseBody);
                    }
                }
            }
            catch (Exception err)
            {
                throw new DetailedLogException("The http request for the feed failed.", err)
                {
                    Fingerprint = "FEED_HTTP_REQUEST_FAILED",
                    Details =
                    {
                        {"feed", feed},
                    }
                };
            }
        }

        private static string GetItemUrl(FeedItem feedItem)
        {
            string linkValue = feedItem.Link?.Trim();
            
            if (!string.IsNullOrWhiteSpace(linkValue) && linkValue.StartsWith("http"))
            {
                return linkValue;
            }

            string idValue = feedItem.Id?.Trim();
            
            if (!string.IsNullOrWhiteSpace(idValue) && idValue.StartsWith("http"))
            {
                return idValue;
            }

            return null;
        }

        private static string GetItemStringID(FeedItem feedItem)
        {
            string idValue = feedItem.Id?.Trim();
            string linkValue = feedItem.Link?.Trim();
            string titleValue = feedItem.Title?.Trim();

            if (!string.IsNullOrWhiteSpace(idValue))
            {
                return $"ID({idValue})";
            }

            if (!string.IsNullOrWhiteSpace(linkValue))
            {
                return $"LINK({linkValue})";
            }

            if (!string.IsNullOrWhiteSpace(titleValue))
            {
                return $"TITLE({titleValue})";
            }

            return null;
        }
    }

    public class ParsedFeed
    {
        public ParsedFeed(int capacity)
        {
            this.Items = new List<ParsedFeedItem>(capacity);
            this.FeedItemHashes = new HashSet<long>(capacity);
        }
        
        public List<ParsedFeedItem> Items { get; } 

        public HashSet<long> FeedItemHashes { get; }
        
        public long FeedHash { get; set; }
    }


    public class ParsedFeedItem
    {
        public FeedItem Item { get; set; }

        public long FeedItemHash { get; set; }
    }

    public class FeedUpdateModel
    {
        public List<FeedItemPoco> NewItems { get; set; }

        public long? NewFeedHash { get; set; }
        
        public FeedPoco Feed { get; set; }
    }
}
