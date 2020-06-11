namespace Newsgirl.Shared.Logging.Consumers
{
    using System;
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Logging;

    public class ElasticsearchLogDataConsumer : LogConsumer<LogData>
    {
        private readonly string indexName;
        private readonly ElasticsearchClient elasticsearchClient;

        public ElasticsearchLogDataConsumer(ErrorReporter errorReporter, ElasticsearchConfig config, string indexName): base(errorReporter)
        {
            this.indexName = indexName;
            this.elasticsearchClient = new ElasticsearchClient(config);
        }
        
        protected override async ValueTask Flush(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                string jsonBody = JsonSerializer.Serialize(data[i].Fields);
            
                var response = await this.elasticsearchClient.IndexAsync<CustomElasticsearchResponse>(this.indexName, jsonBody);

                if (!response.Success)
                {
                    throw new ApplicationException(response.ToString());
                }
            }
        }
    }
    
    public class ElasticsearchConsumer<T> : LogConsumer<T>
    {
        private readonly string indexName;
        private readonly ElasticsearchClient elasticsearchClient;

        public ElasticsearchConsumer(ErrorReporter errorReporter, ElasticsearchConfig config, string indexName): base(errorReporter)
        {
            this.indexName = indexName;
            this.elasticsearchClient = new ElasticsearchClient(config);
        }
        
        protected override async ValueTask Flush(ArraySegment<T> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                string jsonBody = JsonSerializer.Serialize(data[i]);
            
                var response = await this.elasticsearchClient.IndexAsync<CustomElasticsearchResponse>(this.indexName, jsonBody);

                if (!response.Success)
                {
                    throw new ApplicationException(response.ToString());
                }
            }
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
        }
    }
}
