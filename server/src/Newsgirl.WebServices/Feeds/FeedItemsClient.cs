namespace Newsgirl.WebServices.Feeds
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;

    using CodeHollow.FeedReader;

    using Infrastructure;
    using Infrastructure.Data;

    using Newtonsoft.Json;

    public class FeedItemsClient
    {
        public async Task<List<FeedItemBM>> GetFeedItems(string url)
        {
            string feedContent = await GetFeedContent(url);

            var materialized = FeedReader.ReadFromString(feedContent);

            var item = materialized.Items.FirstOrDefault(x => x.Link == null);

            if (item != null)
            {
                await Global.Log.LogError(new DetailedLogException("Links is null.")
                {
                    Context =
                    {
                        {"item", item}
                    }
                });
            }
            
            var items = materialized.Items.Select(x => new FeedItemBM
            {
                FeedItemUrl = x.Link,
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
                                return await content.ReadAsStringAsync();
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
            return new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(30)
            };
        }
    }
}