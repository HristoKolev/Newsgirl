namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.IO;
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
                var log = data[i];

                string json = JsonHelper.Serialize(new Dictionary<string, object>(log.Fields));

                await Console.Out.WriteLineAsync(json);
            }
        }
    }

    /// <summary>
    /// Sends LogData events to ELK.
    /// </summary>
    public class ElasticsearchEventDestination : EventDestination<LogData>
    {
        private readonly string indexName;
        private readonly ElasticsearchClient elasticsearchClient;
        private readonly DateTimeService dateTimeService;

        public ElasticsearchEventDestination(
            ErrorReporter errorReporter,
            DateTimeService dateTimeService,
            ElasticsearchConfig config,
            string indexName) : base(errorReporter)
        {
            this.dateTimeService = dateTimeService;
            this.indexName = indexName;
            this.elasticsearchClient = new ElasticsearchClient(config);
        }

        protected override ValueTask Flush(ArraySegment<LogData> data)
        {
            var array = ArrayPool<Dictionary<string, object>>.Shared.Rent(data.Count);

            try
            {
                for (int i = 0; i < data.Count; i++)
                {
                    data[i].Fields.Add("log_date", this.dateTimeService.CurrentTime().ToString("O"));
                    array[i] = data[i].Fields;
                }

                return this.elasticsearchClient.BulkCreate(
                    this.indexName,
                    new ArraySegment<Dictionary<string, object>>(array, 0, data.Count)
                );
            }
            finally
            {
                ArrayPool<Dictionary<string, object>>.Shared.Return(array);
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
            await using var memStream = new MemoryStream();

            for (int i = 0; i < data.Count; i++)
            {
                await memStream.WriteAsync(this.bulkHeaderBytes);
                await JsonHelper.SerializeGenericType(memStream, data[i]);
                await memStream.WriteAsync(this.bulkNewLineBytes);
            }

            memStream.Position = 0;

            var content = new StreamContent(memStream);

            content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

            var response = await this.httpClient.PostAsync(
                new Uri(index + "/_bulk", UriKind.Relative),
                content
            );

            string responseBody = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                throw new DetailedException("Elasticsearch endpoint returned a non-success status code.")
                {
                    Details =
                    {
                        {"elasticsearchResponseStatusCode", (int) response.StatusCode},
                        {"elasticsearchResponseJson", responseBody},
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
                        {"elasticsearchResponseJson", responseBody},
                    },
                };
            }
        }

        private class ElasticsearchBulkResponse
        {
            public bool Errors { get; set; }
        }
    }

    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
    }
}
