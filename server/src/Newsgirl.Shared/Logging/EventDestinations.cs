namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Threading.Tasks;

    /// <summary>
    /// Prints events to `stdout`.
    /// </summary>
    public class ConsoleEventDestination : EventDestination<LogData>
    {
        public ConsoleEventDestination(ErrorReporter errorReporter) : base(errorReporter) { }

        protected override async ValueTask Flush(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                string json = JsonHelper.Serialize(data[i].Fields);
                await Console.Out.WriteLineAsync(json);
            }
        }
    }

    /// <summary>
    /// Sends <see cref="LogData" /> events to ELK.
    /// </summary>
    public class ElasticsearchEventDestination : EventDestination<LogData>
    {
        private readonly string indexName;
        private readonly ElasticsearchClient elasticsearchClient;

        public ElasticsearchEventDestination(
            ErrorReporter errorReporter,
            ElasticsearchConfig config,
            string indexName) : base(errorReporter)
        {
            this.indexName = indexName;
            this.elasticsearchClient = new ElasticsearchClient(config);
        }

        protected override ValueTask Flush(ArraySegment<LogData> data)
        {
            var buffer = ArrayPool<Dictionary<string, object>>.Shared.Rent(data.Count);

            try
            {
                for (int i = 0; i < data.Count; i++)
                {
                    buffer[i] = data[i].Fields;
                }

                var segment = new ArraySegment<Dictionary<string, object>>(buffer, 0, data.Count);

                return this.elasticsearchClient.BulkCreate(this.indexName, segment);
            }
            finally
            {
                ArrayPool<Dictionary<string, object>>.Shared.Return(buffer);
            }
        }
    }

    /// <summary>
    /// Sends events to ELK.
    /// </summary>
    public class ElasticsearchEventDestination<T> : EventDestination<T>
    {
        private readonly string indexName;
        private readonly ElasticsearchClient elasticsearchClient;

        public ElasticsearchEventDestination(ErrorReporter errorReporter, ElasticsearchConfig config, string indexName) : base(errorReporter)
        {
            this.indexName = indexName;
            this.elasticsearchClient = new ElasticsearchClient(config);
        }

        protected override ValueTask Flush(ArraySegment<T> data)
        {
            return this.elasticsearchClient.BulkCreate(this.indexName, data);
        }
    }

    /// <summary>
    /// A simple client for elasticsearch.
    /// Currently only supports bulk indexing.
    /// </summary>
    public class ElasticsearchClient
    {
        private readonly HttpClient httpClient;
        private readonly byte[] bulkHeaderBytes = EncodingHelper.UTF8.GetBytes("{\"create\":{}}\n");
        private readonly byte[] bulkNewLineBytes = EncodingHelper.UTF8.GetBytes("\n");

        public ElasticsearchClient(HttpClient httpClient, ElasticsearchConfig config)
        {
            this.httpClient = httpClient;

            httpClient.Timeout = TimeSpan.FromMinutes(1);
            httpClient.BaseAddress = new Uri(config.Url);

            string basicToken = Convert.ToBase64String(EncodingHelper.UTF8.GetBytes($"{config.Username}:{config.Password}"));

            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", basicToken);
        }

        public ElasticsearchClient(ElasticsearchConfig config) : this(new HttpClient(), config) { }

        public async ValueTask BulkCreate<T>(string index, ArraySegment<T> data)
        {
            var content = new CustomStreamContent(async stream =>
            {
                for (int i = 0; i < data.Count; i++)
                {
                    await stream.WriteAsync(this.bulkHeaderBytes);
                    await JsonHelper.SerializeGenericType(stream, data[i]);
                    await stream.WriteAsync(this.bulkNewLineBytes);
                }
            });

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await this.httpClient.PostAsync(new Uri(index + "/_bulk", UriKind.Relative), content);

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new DetailedException("Elasticsearch endpoint returned a non-success status code.")
                {
                    Details =
                    {
                        { "elasticsearchResponseStatusCode", (int)response.StatusCode },
                        { "elasticsearchResponseJson", responseBody },
                    },
                };
            }

            var responseDto = JsonHelper.Deserialize<ElasticsearchBulkResponse>(responseBody);

            if (responseDto.Errors)
            {
                throw new DetailedException("Elasticsearch endpoint returned an error.")
                {
                    Details =
                    {
                        { "elasticsearchResponseJson", responseBody },
                    },
                };
            }
        }

        private class ElasticsearchBulkResponse
        {
            public bool Errors { get; set; }
        }
    }

    public class AppInfoEventData
    {
        public virtual string InstanceName { get; set; }

        public virtual string Environment { get; set; }

        public virtual string AppVersion { get; set; }
    }

    public class CustomStreamContent : HttpContent
    {
        private readonly Func<Stream, Task> func;

        public CustomStreamContent(Func<Stream, Task> func)
        {
            this.func = func;
        }

        protected override Task SerializeToStreamAsync(Stream stream, TransportContext context)
        {
            return this.func(stream);
        }

        protected override bool TryComputeLength(out long length)
        {
            length = 0;
            return false;
        }
    }

    /// <summary>
    /// Using this extension method allows us to not specify the stream name and the event data structure.
    /// </summary>
    public static class GeneralLoggingExtensions
    {
        public const string GENERAL_EVENT_STREAM = "GENERAL_LOG";

        public static void General(this Log log, Func<LogData> func)
        {
            log.Log(GENERAL_EVENT_STREAM, func);
        }
    }

    /// <summary>
    /// This is used as a most general log data structure.
    /// </summary>
    public class LogData : AppInfoEventData, IEnumerable
    {
        private const string INSTANCE_NAME_KEY = "instanceName";
        private const string ENVIRONMENT_KEY = "environment";
        private const string APP_VERSION_KEY = "appVersion";

        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

        public LogData(string message)
        {
            this.Fields.Add("message", message);
        }

        /// <summary>
        /// This is not meant to be used explicitly, but with he collection initialization syntax.
        /// </summary>
        public void Add(string key, object val)
        {
            this.Fields.Add(key, val);
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public static implicit operator LogData(string x)
        {
            return new LogData(x);
        }

        public override string InstanceName
        {
            get => (string)this.Fields[INSTANCE_NAME_KEY];
            set => this.Fields[INSTANCE_NAME_KEY] = value;
        }

        public override string Environment
        {
            get => (string)this.Fields[ENVIRONMENT_KEY];
            set => this.Fields[ENVIRONMENT_KEY] = value;
        }

        public override string AppVersion
        {
            get => (string)this.Fields[APP_VERSION_KEY];
            set => this.Fields[APP_VERSION_KEY] = value;
        }
    }

    public class LogPreprocessor : EventPreprocessor
    {
        private readonly DateTimeService dateTimeService;
        private readonly LogPreprocessorConfig logPreprocessorConfig;

        public LogPreprocessor(DateTimeService dateTimeService, LogPreprocessorConfig logPreprocessorConfig)
        {
            this.dateTimeService = dateTimeService;
            this.logPreprocessorConfig = logPreprocessorConfig;
        }

        public void ProcessItem<TData>(ref TData item)
        {
            if (item is LogData logData)
            {
                logData.Fields.Add("log_date", this.dateTimeService.EventTime().ToString("O"));
            }

            if (item is AppInfoEventData appInfoEventData)
            {
                appInfoEventData.AppVersion = this.logPreprocessorConfig.AppVersion;
                appInfoEventData.Environment = this.logPreprocessorConfig.Environment;
                appInfoEventData.InstanceName = this.logPreprocessorConfig.InstanceName;
            }
        }
    }

    public class LogPreprocessorConfig
    {
        public string InstanceName { get; set; }

        public string Environment { get; set; }

        public string AppVersion { get; set; }
    }

    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
