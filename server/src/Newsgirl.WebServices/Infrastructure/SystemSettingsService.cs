using Newsgirl.WebServices.Infrastructure.Data;

namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Newsgirl.WebServices.Infrastructure.Data;

    public class SystemSettingsService
    {
        public SystemSettingsService(IDbService db)
        {
            this.Db = db;
        }

        private IDbService Db { get; }

        public async Task<T> ReadSettings<T>()
            where T : new()
        {
            var modelType = typeof(T);

            var entries = await this.Db.Poco.Filter(new SystemSettingFM());

            var instance = new T();

            foreach (var propertyInfo in modelType.GetProperties())
            {
                var entry = entries.FirstOrDefault(x => x.SettingName == propertyInfo.Name);

                if (entry == null)
                {
                    throw new ApplicationException($"No system_settings entry found for property '{propertyInfo.Name}' of type '{modelType.Name}').");
                }

                object value = Convert.ChangeType(entry.SettingValue, propertyInfo.PropertyType);

                propertyInfo.SetValue(instance, value);
            }

            return instance;
        }
    }
}