using System;
using System.IO;
using System.Threading.Tasks;

using Autofac;
using Newtonsoft.Json;

using Newsgirl.Shared;
using Newsgirl.Shared.Data;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Fetcher
{
    public static class Global
    {
        public static readonly string RootDirectory =
            Path.GetDirectoryName(typeof(Global).Assembly.Location);

        public static readonly string ConfigDirectory =
            Path.Combine(RootDirectory, "../../../");

        public static readonly string AppConfigLocation =
            Path.Combine(ConfigDirectory, $"{typeof(Global).Assembly.GetName().Name}.json");
        
        public static readonly string AppVersion = typeof(Global).Assembly.GetName().Version.ToString();

        public static AppConfig AppConfig { get; set; }
        
        public static SystemSettingsModel SystemSettings { get; set; }
        
        public static ILog Log { get; set; }
        
        public static async Task ReloadStartupConfig()
        {
            try
            {
                Log.Log("Reloading config...");

                await LoadStartupConfig();
            }
            catch (Exception exception)
            {
                await Log.Error(exception);
            }
        }

        public static async Task LoadStartupConfig()
        {
            AppConfig = JsonConvert.DeserializeObject<AppConfig>(
                await File.ReadAllTextAsync(AppConfigLocation));
            AppConfig.Logging.Release = AppVersion;
            Log = new CustomLogger(AppConfig.Logging);
        }
    }

    public class AppConfig
    {
        public string ConnectionString { get; set; }
        
        public CustomLoggerConfig Logging { get; set; }
    }
    
    public class FetcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Globally managed
            builder.Register((c, p) => Global.SystemSettings);
            builder.Register((c, p) => Global.Log);
            
            // Single instance
            builder.RegisterType<Hasher>().SingleInstance();
            builder.RegisterType<FeedContentProvider>().As<IFeedContentProvider>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            // Per scope
            builder.Register((c, p) => 
                    DbFactory.CreateConnection(Global.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();

            builder.RegisterType<DbService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedItemsImportService>().As<IFeedItemsImportService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedFetcher>().InstancePerLifetimeScope();

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
            await Global.LoadStartupConfig();

            try
            {
                using (new FileWatcher(Global.AppConfigLocation, Global.ReloadStartupConfig))
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
            }
            catch (Exception exception)
            {
                await Global.Log.Error(exception);
                
                return 1;
            }
        }
    }
}
