namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Data;

    using LinqToDB;

    public class SystemSettingsService
    {
        public SystemSettingsService(IDbService db)
        {
            this.Db = db;
        }

        private IDbService Db { get; }

        /// <summary>
        /// Reads the settings from the database.
        /// </summary>
        public async Task<T> ReadSettings<T>()
            where T : new()
        {
            var modelType = typeof(T);

            var entries = await this.Db.Poco.SystemSettings.ToArrayAsync();

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
}