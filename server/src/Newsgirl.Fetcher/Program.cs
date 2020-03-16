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
        private static readonly string AppVersion = typeof(Global).Assembly.GetName().Version.ToString();

        public static string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public static AppConfig AppConfig { get; set; }
        
        public static SystemSettingsModel SystemSettings { get; set; }
        
        public static ILog Log { get; set; }

        private static FileWatcher AppConfigWatcher { get; set; }

        private static IContainer IoC { get; set; }

        private static async Task ReloadStartupConfig()
        {
            try
            {
                Log.Log("Reloading config...");

                await LoadConfig();
            }
            catch (Exception exception)
            {
                await Log.Error(exception);
            }
        }
        
        private static async Task LoadConfig()
        {
            AppConfig = JsonConvert.DeserializeObject<AppConfig>(await File.ReadAllTextAsync(AppConfigPath));

            AppConfig.Logging.Release = AppVersion;

            Log = new CustomLogger(AppConfig.Logging);
        }

        public static async Task InitializeAsync()
        {
            await LoadConfig();

            AppConfigWatcher = new FileWatcher(AppConfigPath, ReloadStartupConfig);
            
            IoC = IoCFactory.Create();
            
            var systemSettingsService = IoC.Resolve<SystemSettingsService>();
                
            SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }

        public static async Task DisposeAsync()
        {
            AppConfigWatcher?.Dispose();
            AppConfigWatcher = null;
            
            if (IoC != null)
            {
                await IoC.DisposeAsync();
                IoC = null;
            }

            AppConfig = null;

            SystemSettings = null;

            Log = null;
        }

        public static async Task RunCycleAsync()
        {
            var fetcherInstance = IoC.Resolve<FeedFetcher>();
            
            await fetcherInstance.FetchFeeds();

            var log = IoC.Resolve<ILog>();
            
            log.Log($"Waiting {SystemSettings.FetcherCyclePause} seconds...");
            
            await Task.Delay(TimeSpan.FromSeconds(SystemSettings.FetcherCyclePause));
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
            try
            {
                await Global.InitializeAsync();
                
                while (true)
                {
                    await Global.RunCycleAsync();
                }
            }
            catch (Exception exception)
            {
                if (Global.Log != null)
                {
                    await Global.Log.Error(exception);                    
                }

                return 1;
            }
            finally
            {
                await Global.DisposeAsync();
            }
        }
    }
}
