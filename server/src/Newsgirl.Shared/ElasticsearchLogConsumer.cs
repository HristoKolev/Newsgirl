namespace Newsgirl.Shared
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Elasticsearch.Net;

    public class ElasticsearchLogDataConsumer : LogConsumer<LogData>
    {
        private readonly string indexName;
        private readonly ElasticLowLevelClient elasticsearchClient;

        public ElasticsearchLogDataConsumer(ErrorReporter errorReporter, ElasticsearchConfig config, string indexName): base(errorReporter)
        {
            this.indexName = indexName;

            var elasticConnectionConfiguration = new ConnectionConfiguration(new Uri(config.Url));
            elasticConnectionConfiguration.BasicAuthentication(config.Username, config.Password);
            this.elasticsearchClient = new ElasticLowLevelClient(elasticConnectionConfiguration);
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
        private readonly ElasticLowLevelClient elasticsearchClient;

        public ElasticsearchConsumer(ErrorReporter errorReporter, ElasticsearchConfig config, string indexName): base(errorReporter)
        {
            this.indexName = indexName;

            var elasticConnectionConfiguration = new ConnectionConfiguration(new Uri(config.Url));
            elasticConnectionConfiguration.BasicAuthentication(config.Username, config.Password);
            this.elasticsearchClient = new ElasticLowLevelClient(elasticConnectionConfiguration);
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
        
    public class CustomElasticsearchResponse : ElasticsearchResponseBase
    {
    }
}
