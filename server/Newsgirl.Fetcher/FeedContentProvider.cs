namespace Newsgirl.Fetcher;

using System;
using System.Net.Http;
using System.Threading.Tasks;
using Shared;

public class FeedContentProvider : IFeedContentProvider
{
    private readonly HttpClient httpClient;

    public FeedContentProvider(FetcherAppConfig appConfig)
    {
        this.httpClient = new HttpClient
        {
            Timeout = TimeSpan.FromSeconds(appConfig.HttpClientRequestTimeout),
            DefaultRequestVersion = new Version(2, 0),
        };

        this.httpClient.DefaultRequestHeaders.UserAgent.ParseAdd(appConfig.HttpClientUserAgent);
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
