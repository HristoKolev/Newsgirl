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

            try
            {
                materialized = FeedReader.ReadFromString(feedContent);
            }
            catch (Exception exception)
            {
                throw new DetailedLogException("Failed to parse feed content.",
                                               exception)
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
                FeedItemUrl = string.IsNullOrWhiteSpace(x.Link) ? null : x.Link,
                FeedItemTitle = x.Title,
            }).ToList();

            return items;
        }

        private static async Task<string> GetFeedContent(string url)
        {
            using (var httpClient = CreateHttpClient())
            {
                var request = FeedRequest(url);

                using (var response = await httpClient.SendAsync(request))
                {
                    if (response.IsSuccessStatusCode)
                    {
                        using (var content = response.Content)
                        {
                            try
                            {
                                var responseBody = await content.ReadAsByteArrayAsync();

                                return Encoding.UTF8.GetString(responseBody);
                            }
                            catch (Exception exception)
                            {
                                throw new DetailedLogException("Failed to read the response to the feed request.",
                                                               exception)
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

                    throw new DetailedLogException("The feed request failed.")
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

        private static HttpRequestMessage FeedRequest(string url)
        {
            return new HttpRequestMessage(HttpMethod.Get, url);
        }

        private static HttpClient CreateHttpClient()
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(60)
            };
            
            client.DefaultRequestHeaders.UserAgent.ParseAdd("Mozilla/5.0 (X11; Linux x86_64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/72.0.3626.81 Safari/537.36");
                 
            return client ;
        }
    }
}