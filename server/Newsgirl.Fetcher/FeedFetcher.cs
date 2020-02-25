using System;
using System.Collections.Generic;
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

        public FeedFetcher(DbService db)
        {
            this.db = db;
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

        private async Task<IEnumerable<FeedItemPoco>> ProcessFeed(FeedPoco feed)
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

                return materialized.Items.Select(x => new FeedItemPoco
                {
                    FeedItemUrl = x.Link.SomethingOrNull(),
                    FeedItemTitle = x.Title.SomethingOrNull(),
                    FeedItemDescription = x.Description.SomethingOrNull(),
                });
            }
            catch (Exception err)
            {
                MainLogger.Error(err);
                
                return Enumerable.Empty<FeedItemPoco>();
            }
        }
    }
}
