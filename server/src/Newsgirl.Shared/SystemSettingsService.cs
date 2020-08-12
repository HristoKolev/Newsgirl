namespace Newsgirl.Shared
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using LinqToDB;

    public class SystemSettingsService
    {
        private readonly DbService db;

        public SystemSettingsService(DbService db)
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

            foreach (var propertyInfo in modelType.GetProperties())
            {
                var entry = entries.FirstOrDefault(x => x.SettingName == propertyInfo.Name);

                if (entry == null)
                {
                    throw new ApplicationException(
                        $"No system_settings entry found for property '{propertyInfo.Name}' of type '{modelType.Name}').");
                }

                var value = Convert.ChangeType(entry.SettingValue, propertyInfo.PropertyType);

                propertyInfo.SetValue(instance, value);
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
    }
}
