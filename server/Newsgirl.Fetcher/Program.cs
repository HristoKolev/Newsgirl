using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Autofac;
using Newsgirl.Shared;
using Newsgirl.Shared.Data;
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

        public static bool Debug => AppConfig.Debug.General;
    }

    public class AppConfig
    {
        public string SentryDsn { get; set; }

        public string Environment { get; set; }
        
        public string ConnectionString { get; set; }
        
        public DebugConfig Debug { get; set; }
    }

    public class DebugConfig
    {
        public bool General { get; set; }
    }
    
    public class FetcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register((c, p) => 
                    DbFactory.CreateConnection(Global.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();

            builder.RegisterType<DbService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<FeedFetcher>();
            
            base.Load(builder);
        }
    }
    
    public static class IoCFactory
    {
        public static IContainer Create()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<SharedModule>();
            builder.RegisterModule<FetcherModule>();

            return builder.Build();
        }
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
                Environment = Global.AppConfig.Environment,
                DebugLogging = Global.Debug,
            };
                
            using (MainLogger.Initialize(loggingConfig))
            {
                try
                {
                    while (true)
                    {
                        await using (var container = IoCFactory.Create())
                        {
                            var systemSettingsService = container.Resolve<SystemSettingsService>();
                        
                            Global.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
                        
                            var fetcherInstance = container.Resolve<FeedFetcher>();

                            await fetcherInstance.FetchFeeds();    
                        }

                        await Task.Delay(TimeSpan.FromSeconds(Global.SystemSettings.FetcherCyclePause));                        
                    }
                }
                catch (Exception exception)
                {
                    MainLogger.Error(exception);

                    return 1;
                }
            }
        }
    }
}
