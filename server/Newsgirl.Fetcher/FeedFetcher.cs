using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
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
            
            
        }

        public async Task ProcessFeed()
        {
            
        }
    }
    
    public class FeedItemsClientService
    {
        public async Task<List<FeedItemPoco>> FetchFeedItems(string url)
        {
            string feedContent = await GetFeedContent(url);

            Feed materialized;

            try
            {
                materialized = FeedReader.ReadFromString(feedContent);
            }
            catch (Exception exception)
            {
                throw new DetailedLogException("Failed to parse feed content.", exception)
                {
                    Details =
                    {
                        {"url", url},
                        {"content", feedContent},
                    }
                };
            }
            
            var items = materialized.Items.Select(x => new FeedItemPoco
            {
                FeedItemUrl = x.Link.SomethingOrNull(),
                FeedItemTitle = x.Title,
                FeedItemDescription = x.Description.SomethingOrNull()
            }).ToList();

            return items;
        }

        private static async Task<string> GetFeedContent(string url)
        {
            using (var httpClient = CreateHttpClient())
            {
                var request = new HttpRequestMessage(HttpMethod.Get, url);

                using (var response = await httpClient.SendAsync(request))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        throw new DetailedLogException("The feed request failed.")
                        {
                            Details =
                            {
                                {"request", request},
                                {"response", response},
                            }
                        };
                    }

                    using (var content = response.Content)
                    {
                        try
                        {
                            var responseBody = await content.ReadAsByteArrayAsync();

                            return Encoding.UTF8.GetString(responseBody);
                        }
                        catch (Exception exception)
                        {
                            throw new DetailedLogException("Failed to read the response to the feed request.", exception)
                            {
                                Details =
                                {
                                    {"request", request},
                                    {"response", response},
                                }
                            };
                        }
                    }
                }
            }
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Global.Settings.HttpClientUserAgent);
                 
            return client ;
        }
    }
}