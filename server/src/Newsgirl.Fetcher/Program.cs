namespace Newsgirl.Fetcher
{
    using System;
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Data;
    using Shared.Infrastructure;

    public class FetcherApp : IAsyncDisposable
    {
        // ReSharper disable once InconsistentNaming
        private readonly string AppVersion = typeof(FetcherApp).Assembly.GetName().Version?.ToString();

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public FetcherAppConfig AppConfig { get; private set; }

        public SystemSettingsModel SystemSettings { get; private set; }

        public ILog Log { get; private set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; private set; }

        public async ValueTask DisposeAsync()
        {
            this.AppConfigWatcher?.Dispose();
            this.AppConfigWatcher = null;

            if (this.IoC != null)
            {
                await this.IoC.DisposeAsync();
                this.IoC = null;
            }

            this.AppConfig = null;

            this.SystemSettings = null;

            this.Log = null;
        }

        private async Task ReloadStartupConfig()
        {
            try
            {
                await this.Log.Log("Reloading config...");

                await this.LoadConfig();
            }
            catch (Exception exception)
            {
                await this.Log.Error(exception);
            }
        }

        private async Task LoadConfig()
        {
            this.AppConfig =
                JsonConvert.DeserializeObject<FetcherAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));

            this.AppConfig.Logging.Release = this.AppVersion;

            this.Log = new CustomLogger(this.AppConfig.Logging);
        }

        public async Task InitializeAsync()
        {
            await this.LoadConfig();

            this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, this.ReloadStartupConfig);

            var builder = new ContainerBuilder();

            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new FetcherIoCModule(this));

            this.IoC = builder.Build();

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();

            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }

        public async Task RunCycleAsync()
        {
            var fetcherInstance = this.IoC.Resolve<FeedFetcher>();

            await fetcherInstance.FetchFeeds();

            var log = this.IoC.Resolve<ILog>();

            await log.Log($"Waiting {this.SystemSettings.FetcherCyclePause} seconds...");

            await Task.Delay(TimeSpan.FromSeconds(this.SystemSettings.FetcherCyclePause));
        }
    }

    public class FetcherAppConfig
    {
        public string ConnectionString { get; set; }

        public CustomLoggerConfig Logging { get; set; }
    }

    public class FetcherIoCModule : Module
    {
        private readonly FetcherApp app;

        public FetcherIoCModule(FetcherApp app)
        {
            this.app = app;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Globally managed
            builder.Register((c, p) => this.app.SystemSettings);
            builder.Register((c, p) => this.app.Log);

            // Single instance
            builder.RegisterType<Hasher>().SingleInstance();
            builder.RegisterType<FeedContentProvider>().As<IFeedContentProvider>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            // Per scope
            builder.Register((c, p) =>
                    DbFactory.CreateConnection(this.app.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();

            builder.RegisterType<DbService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedItemsImportService>().As<IFeedItemsImportService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedFetcher>().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }

    public static class Program
    {
        private static async Task<int> Main()
        {
            await using (var app = new FetcherApp())
            {
                try
                {
                    await app.InitializeAsync();

                    while (true)
                    {
                        await app.RunCycleAsync();
                    }
                }
                catch (Exception exception)
                {
                    if (app.Log != null)
                    {
                        await app.Log.Error(exception);
                    }

                    return 1;
                }
            }
        }
    }
}
