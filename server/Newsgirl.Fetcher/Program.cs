using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Newsgirl.Shared;
using Newsgirl.Shared.Infrastructure;
using Newtonsoft.Json;

namespace Newsgirl.Fetcher
{
    public static class Global
    {
        public static readonly string RootDirectory =
            Path.GetDirectoryName(Assembly.GetEntryAssembly()?.Location);

        public static readonly string ConfigDirectory =
            Path.Combine(RootDirectory, "../../../");

        public static readonly string AppConfigLocation =
            Path.Combine(ConfigDirectory, $"{Assembly.GetEntryAssembly()?.GetName().Name}.json");

        public static AppConfig AppConfig { get; set; }
        
        public static SystemSettingsModel SystemSettings { get; set; }
    }

    public class AppConfig
    {
        public string SentryDsn { get; set; }

        public string Environment { get; set; }
        
        public string ConnectionString { get; set; }
    }
    
    public static class Program
    {
        private static async Task<int> Main()
        {
            Global.AppConfig = JsonConvert.DeserializeObject<AppConfig>(
                await File.ReadAllTextAsync(Global.AppConfigLocation));

            var loggingConfig = new LoggerConfigModel
            {
                SentryDsn = Global.AppConfig.SentryDsn,
                ConfigDirectory = Global.ConfigDirectory,
                Environment = Global.AppConfig.Environment
            };

            using var loggerHandle = MainLogger.Initialize(loggingConfig);
            
            try
            {
                await using var container = IoCFactory.Create();
                
                var systemSettingsService = container.Resolve<SystemSettingsService>();
                        
                Global.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
                        
                var fetcherInstance = container.Resolve<FeedFetcher>();

                await fetcherInstance.FetchFeeds();

                return 0;
            }
            catch (Exception exception)
            {
                MainLogger.Error(exception);

                return 1;
            }
        }
    }
}