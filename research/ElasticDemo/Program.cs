namespace ElasticDemo
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Elasticsearch.Net;

    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
        
        public string IndexName { get; set; }
    }

    public class CustomElasticsearchClient
    {
        private readonly ElasticsearchConfig config;
        private readonly ElasticLowLevelClient innerClient;

        public CustomElasticsearchClient(ElasticsearchConfig config)
        {
            this.config = config;
            var connectionConfiguration = new ConnectionConfiguration(new Uri(config.Url));
            connectionConfiguration.BasicAuthentication(config.Username, config.Password);
            var client = new ElasticLowLevelClient(connectionConfiguration);

            this.innerClient = client;
        }

        public Task SendLog(string message, Dictionary<string, object> fields = null)
        {
            fields ??= new Dictionary<string, object>();
            fields.Add("message", message);
            
            return SendLog(fields);
        }
        
        public async Task SendLog(Dictionary<string, object> fields)
        {
            fields.Add("log_date", DateTime.UtcNow.ToString("O"));
            
            string jsonBody = JsonSerializer.Serialize(fields);
            
            var response = await this.innerClient.IndexAsync<CustomElasticsearchResponse>(this.config.IndexName, jsonBody);

            if (!response.Success)
            {
                throw new ApplicationException(response.ToString());
            }
        }

        private class CustomElasticsearchResponse : ElasticsearchResponseBase
        {
        }
    }
    
    public static class Program
    {
        private static async Task Main(string[] args)
        {
            var elasticConfig = new ElasticsearchConfig
            {
                Url = "http://192.168.0.107:9200",
                Username = "elastic",
                Password = "changeme",
                IndexName = "hristo_logs",
            };

            var client = new CustomElasticsearchClient(elasticConfig);

            for (int i = 0; i < 100; i++)
            {
                await client.SendLog("mess " + i, new Dictionary<string, object>
                {
                    {"cat123", "bla"}
                });
            }
        }
    }
}
