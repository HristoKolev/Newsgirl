namespace Newsgirl.Shared
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using LinqToDB;
    using Logging;
    using Postgres;

    public class SystemSettingsService
    {
        private readonly IDbService db;

        public SystemSettingsService(IDbService db)
        {
            this.db = db;
        }

        /// <summary>
        /// Reads the settings from the database.
        /// </summary>
        public async Task<T> ReadSettings<T>() where T : new()
        {
            var modelType = typeof(T);

            var entries = await this.db.Poco.SystemSettings.ToArrayAsync();

            var instance = new T();

            foreach (var prop in modelType.GetProperties())
            {
                var entry = entries.FirstOrDefault(x => x.SettingName == prop.Name);

                if (entry == null)
                {
                    throw new ApplicationException(
                        $"No system_settings entry found for property '{prop.Name}' of type '{modelType.Name}').");
                }

                object value = null;

                if (entry.SettingValue != null)
                {
                    value = JsonHelper.Deserialize(entry.SettingValue, prop.PropertyType);
                }

                prop.SetValue(instance, value);
            }

            return instance;
        }
    }

    /// <summary>
    /// Settings read from the database.
    /// </summary>
    public class SystemSettingsModel
    {
        /// <summary>
        /// The UserAgent used for http calls to the RSS endpoints.
        /// </summary>
        public string HttpClientUserAgent { get; set; }

        /// <summary>
        /// The timeout for the http calls.
        /// </summary>
        public int HttpClientRequestTimeout { get; set; }

        /// <summary>
        /// The pause between fetch cycles.
        /// </summary>
        public int FetcherCyclePause { get; set; }

        public bool ParallelFeedFetching { get; set; }

        /// <summary>
        /// The pfx certificate that is used to create JWT tokens.
        /// </summary>
        public byte[] SessionCertificate { get; set; }

        public HttpServerAppLoggingConfig HttpServerAppLoggingConfig { get; set; }

        public FetcherAppLoggingConfig FetcherAppLoggingConfig { get; set; }
    }

    public class HttpServerAppLoggingConfig
    {
        public EventStreamConfig[] StructuredLogger { get; set; }

        public ElasticsearchConfig Elasticsearch { get; set; }

        public HttpServerAppElkIndexConfig ElkIndexes { get; set; }
    }

    public class HttpServerAppElkIndexConfig
    {
        public string GeneralLogIndex { get; set; }

        public string HttpLogIndex { get; set; }
    }

    public class FetcherAppLoggingConfig
    {
        public EventStreamConfig[] StructuredLogger { get; set; }

        public ElasticsearchConfig Elasticsearch { get; set; }

        public FetcherAppElkIndexConfig ElkIndexes { get; set; }
    }

    public class FetcherAppElkIndexConfig
    {
        public string GeneralLogIndex { get; set; }

        public string FetcherLogIndex { get; set; }
    }
}
