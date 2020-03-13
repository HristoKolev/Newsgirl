using System;
using System.Net.Http;
using System.Threading.Tasks;

using Newsgirl.Shared;
using Newsgirl.Shared.Data;

namespace Newsgirl.Fetcher
{
    public class FeedContentProvider : IFeedContentProvider
    {
        private readonly HttpClient httpClient;

        public FeedContentProvider(SystemSettingsModel systemSettings)
        {
            this.httpClient = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(systemSettings.HttpClientRequestTimeout),
            };

            this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(systemSettings.HttpClientUserAgent);
        }

        public async Task<byte[]> GetFeedContent(FeedPoco feed)
        {
            using (var response = await this.httpClient.GetAsync(feed.FeedUrl))
            {
                response.EnsureSuccessStatusCode();

                using (var content = response.Content)
                {
                    var responseBody = await content.ReadAsByteArrayAsync();

                    return responseBody;
                }
            }
        }
    }
    
    public interface IFeedContentProvider
    {
        Task<byte[]> GetFeedContent(FeedPoco feed);
    }
}
