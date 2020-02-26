using System;
using System.Collections.Generic;
using System.Data.HashFunction.xxHash;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

using CodeHollow.FeedReader;
using LinqToDB;

using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public class FeedFetcher
    {
        private readonly DbService db;
        private readonly AsyncLock dbLock;
        private readonly DateTime fetchTime;
        private readonly IxxHash xxHash;

        public FeedFetcher(DbService db)
        {
            this.db = db;
            this.dbLock = new AsyncLock();
            this.fetchTime = DateTime.UtcNow;
            this.xxHash = xxHashFactory.Instance.Create(new xxHashConfig
            {
                HashSizeInBits = 64
            });
        }

        public async Task FetchFeeds()
        {
            var feeds = await this.db.Poco.Feeds.ToListAsync();
            
            var tasks = feeds.Select(this.ProcessFeed).ToList();
            
            var items = (await Task.WhenAll(tasks)).SelectMany(x => x).ToList();
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
                var feedContent = await GetFeedContent(feed.FeedUrl);

                Feed materialized;

                try
                {
                    materialized = FeedReader.ReadFromString(feedContent);
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

                var materializedItems = materialized.Items as List<FeedItem>;

                long calculatedHash = GetItemsHash(materializedItems);

                if (feed.FeedHash == calculatedHash)
                {
                    return new FeedUpdateModel
                    {
                        Feed = feed,
                        NewItems = null,
                        NewItemsHash = null, 
                    };
                }

                using (await this.dbLock.Lock())
                {
                    
                }
                
                var items = new List<FeedItemPoco>(materializedItems.Count);

                for (int i = 0; i < materializedItems.Count; i++)
                {
                    var materializedItem = materializedItems[i];
                    
                    items.Add(new FeedItemPoco
                    {
                        FeedID = feed.FeedID,
                        FeedItemUrl = materializedItem.Link.SomethingOrNull(),
                        FeedItemTitle = materialized.Title.SomethingOrNull(),
                        FeedItemDescription = materialized.Description.SomethingOrNull(),
                        FeedItemAddedTime = this.fetchTime,
                    });
                }
                
                
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

    // https://stackoverflow.com/questions/6533029/how-to-compare-two-arrays-and-pick-only-the-non-matching-elements-in-postgres
    
    public class FeedUpdateModel
    {
        public List<FeedItemPoco> NewItems { get; set; }

        public long? NewItemsHash { get; set; }
        
        public FeedPoco Feed { get; set; }
    }

    public class FeedItemUrlLookup
    {
        public int FeedItemID { get; set; }

        public string FeedItemUrl { get; set; }
    }
}
