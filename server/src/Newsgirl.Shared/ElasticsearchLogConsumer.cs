namespace Newsgirl.Shared
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Elasticsearch.Net;

    public class ElasticsearchLogConsumer : LogConsumerBase<LogData>
    {
        private readonly ElasticsearchConfig config;
        private readonly ElasticLowLevelClient elasticsearchClient;

        public ElasticsearchLogConsumer(ElasticsearchConfig config, ErrorReporter errorReporter): base(errorReporter)
        {
            this.config = config;
            
            var elasticConnectionConfiguration = new ConnectionConfiguration(new Uri(this.config.Url));
            elasticConnectionConfiguration.BasicAuthentication(this.config.Username, this.config.Password);
            this.elasticsearchClient = new ElasticLowLevelClient(elasticConnectionConfiguration);
        }
        
        protected override async ValueTask ProcessBatch(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var fields = data[i].Fields;
                
                fields.Add("log_date", DateTime.UtcNow.ToString("O"));
            
                string jsonBody = JsonSerializer.Serialize(fields);
            
                var response = await this.elasticsearchClient.IndexAsync<CustomElasticsearchResponse>(this.config.IndexName, jsonBody);

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
        
        public string IndexName { get; set; }
    }
        
    public class CustomElasticsearchResponse : ElasticsearchResponseBase
    {
    }
}
