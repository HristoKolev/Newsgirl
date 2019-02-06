namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Text;
    using System.Threading.Tasks;

    using CodeHollow.FeedReader;

    using Infrastructure;
    using Infrastructure.Data;

    public class FeedItemsClient
    {
        public async Task<List<FeedItemBM>> GetFeedItems(string url)
        {
            string feedContent = await GetFeedContent(url);

            Feed materialized;

            if (url == "http://www.pcper.com/rss/podcasts.rss")
            {
                
            }

            try
            {
                materialized = FeedReader.ReadFromString(feedContent);
            }
            catch (Exception exception)
            {
                throw new DetailedLogException("Failed to parse feed content.", exception)
                {
                    Context =
                    {
                        {"url", url},
                        {"content", feedContent},
                    }
                };
            }
            
            var items = materialized.Items.Select(x => new FeedItemBM
            {
                FeedItemUrl = GetFeedItemUrl(x),
                FeedItemTitle = x.Title,
            }).ToList();

            return items;
        }

        private static string GetFeedItemUrl(FeedItem item)
        {
            if (!string.IsNullOrWhiteSpace(item.Link))
            {
                return item.Link;
            }

            //return null;

            string feedItemUrl = item.SpecificItem.Element.Element("guid")?.Value;

            Console.WriteLine(feedItemUrl);
            
            return feedItemUrl;
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
                            Context =
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
                                Context =
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
            
            client.DefaultRequestHeaders.UserAgent.ParseAdd(Global.AppConfig.HttpClientUserAgent);
                 
            return client ;
        }
    }
}