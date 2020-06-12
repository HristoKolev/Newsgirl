using System;

namespace elk_client
{
    using System.Net.Http;
    using System.Threading.Tasks;

    class Program
    {
        static void Main(string[] args)
        {
        }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }

    public class ElasticsearchClient
    {
        private readonly HttpClient httpClient;
        private readonly ElasticsearchConfig config;

        public ElasticsearchClient(HttpClient httpClient, ElasticsearchConfig config)
        {
            this.httpClient = httpClient;
            this.config = config;
        }

        public ElasticsearchClient(ElasticsearchConfig config) : this(new HttpClient(), config)
        {
        }

        public Task Index(string index)
        {
            return Task.CompletedTask;
        }
    }
}
