namespace Newsgirl.Shared.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Elasticsearch.Net;

    public class CustomLogger : ILog
    {
        private readonly CustomLoggerConfig config;
        private readonly ErrorReporter errorReporter;
        //private readonly Channel<Dictionary<string, object>> logChannel = Channel.CreateUnbounded<Dictionary<string, object>>();
        private ElasticLowLevelClient elasticsearchClient;

        public CustomLogger(CustomLoggerConfig config, ErrorReporter errorReporter)
        {
            this.config = config;
            this.errorReporter = errorReporter;

            if (!this.config.DisableElasticsearchIntegration)
            {
                this.CreateElasticsearchClient();
            }
        }

        private void CreateElasticsearchClient()
        {
            if (this.elasticsearchClient != null)
            {
                return;
            }

            var elasticConnectionConfiguration = new ConnectionConfiguration(new Uri(this.config.ElasticsearchConfig.Url));
            elasticConnectionConfiguration.BasicAuthentication(this.config.ElasticsearchConfig.Username, this.config.ElasticsearchConfig.Password);
            this.elasticsearchClient = new ElasticLowLevelClient(elasticConnectionConfiguration);
        }

        public Task Debug(Func<ILog, Task> func)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            return func(this);
        }

        public Task Warn(Func<ILog, Task> func)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            return func(this);
        }

        public Task Log(string message)
        {
            return this.Log(message, null);
        }

        public async Task Log(string message, Dictionary<string, object> fields)
        {
            if (!this.config.DisableConsoleLogging)
            {
                await Console.Out.WriteLineAsync(message);
            }
            
            fields ??= new Dictionary<string, object>();
            fields.Add("message", message);

            await SendToElasticsearch(fields);

            // if (!this.logChannel.Writer.TryWrite(fields))
            // {
            //     throw new NotSupportedException("logChannel.Writer.TryWrite returned false.");
            // }
        }

        public Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo)
        {
            return this.errorReporter.Error(exception, fingerprint, additionalInfo);
        }

        public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
        {
            return this.Error(exception, null, additionalInfo);
        }

        public Task<string> Error(Exception exception, string fingerprint)
        {
            return this.Error(exception, fingerprint, null);
        }

        public Task<string> Error(Exception exception)
        {
            return this.Error(exception, null, null);
        }

        private async Task SendToElasticsearch(Dictionary<string, object> fields)
        {
            this.CreateElasticsearchClient();
            
            fields.Add("log_date", DateTime.UtcNow.ToString("O"));
            
            string jsonBody = JsonSerializer.Serialize(fields);
            
            var response = await this.elasticsearchClient.IndexAsync<CustomElasticsearchResponse>(this.config.ElasticsearchConfig.IndexName, jsonBody);

            if (!response.Success)
            {
                throw new ApplicationException(response.ToString());
            }
        }
    }

    public interface ILog
    {
        Task Debug(Func<ILog, Task> func);
        
        Task Warn(Func<ILog, Task> func);

        Task Log(string message);
        
        Task Log(string message, Dictionary<string, object> fields);

        Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo);
        
        Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo);
        
        Task<string> Error(Exception exception, string fingerprint);
        
        Task<string> Error(Exception exception);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class CustomLoggerConfig
    {
        public bool DisableSentryIntegration { get; set; }

        public bool DisableConsoleLogging { get; set; }

        public bool EnableDebug { get; set; }

        public string ServerName { get; set; }

        public string Environment { get; set; }

        public string SentryDsn { get; set; }

        public string Release { get; set; }
        
        public bool DisableElasticsearchIntegration { get; set; }

        public ElasticsearchConfig ElasticsearchConfig { get; set; }
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
