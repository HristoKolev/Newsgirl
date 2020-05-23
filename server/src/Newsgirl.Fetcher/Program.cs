namespace Newsgirl.Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Logging;

    public class FetcherApp : IAsyncDisposable
    {
        // ReSharper disable once InconsistentNaming
        private readonly string AppVersion = typeof(FetcherApp).Assembly.GetName().Version?.ToString();

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public FetcherAppConfig AppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public StructuredLogger Log { get; set; }
        
        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        public async Task Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskSchedulerOnUnobservedTaskException;

            await this.LoadConfig();
            
            var loggerBuilder = new StructuredLoggerBuilder();
            
            loggerBuilder.AddConfig(GeneralLoggingExtensions.GeneralKey, new Dictionary<string,Func<LogConsumer<LogData>>>
            {
                {"ConsoleConsumer", () => new ConsoleLogDataConsumer(this.ErrorReporter)},
                {"ElasticsearchConsumer", () => new ElasticsearchLogDataConsumer(
                    this.ErrorReporter, 
                    this.AppConfig.Logging.Elasticsearch,
                    this.AppConfig.Logging.ElasticsearchIndexes.GeneralLogIndex
                )},
            });
            
            this.Log = loggerBuilder.Build();
            
            await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);

            this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, this.ReloadStartupConfig);

            var builder = new ContainerBuilder();
            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new FetcherIoCModule(this));
            this.IoC = builder.Build();

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }
        
        private async Task LoadConfig()
        {
            this.AppConfig = JsonConvert.DeserializeObject<FetcherAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            this.AppConfig.ErrorReporter.Release = this.AppVersion;
            
            // If ErrorReporter is not ErrorReporterImpl - do not replace it. Done for testing purposes.
            if (this.ErrorReporter == null || this.ErrorReporter is ErrorReporterImpl)
            {
                this.ErrorReporter = new ErrorReporterImpl(this.AppConfig.ErrorReporter);
            }
        }
        
        private async Task ReloadStartupConfig()
        {
            try
            {
                this.Log.General(() => new LogData("Reloading config..."));
                await this.LoadConfig();
                await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);
            }
            catch (Exception exception)
            {
                await this.ErrorReporter.Error(exception);
            }
        }
        
        private async void TaskSchedulerOnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            await this.ErrorReporter.Error(e.Exception?.InnerException);
        }

        private async void CurrentDomainOnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await this.ErrorReporter.Error((Exception) e.ExceptionObject);
        }
        
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
            
            await this.Log.DisposeAsync();
            this.Log = null;
            
            AppDomain.CurrentDomain.UnhandledException -= this.CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException -= this.TaskSchedulerOnUnobservedTaskException;
            
            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this.ErrorReporter is IAsyncDisposable disposableErrorReporter)
            {
                await disposableErrorReporter.DisposeAsync();
            }

            this.ErrorReporter = null;
        }

        public async Task RunCycle()
        {
            var fetcherInstance = this.IoC.Resolve<FeedFetcher>();

            await fetcherInstance.FetchFeeds();

            this.Log.General(() => new LogData($"Waiting {this.SystemSettings.FetcherCyclePause} seconds..."));

            await Task.Delay(TimeSpan.FromSeconds(this.SystemSettings.FetcherCyclePause));
        }
    }

    public class FetcherAppConfig
    {
        public string ConnectionString { get; set; }

        public ErrorReporterConfig ErrorReporter { get; set; }
        
        public LoggingConfig Logging { get; set; }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class LoggingConfig
    {
        public StructuredLoggerConfig[] StructuredLogger { get; set; }
        
        public ElasticsearchConfig Elasticsearch { get; set; }

        public ElasticsearchIndexConfig ElasticsearchIndexes { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class ElasticsearchIndexConfig
    {
        public string GeneralLogIndex { get; set; }
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
            builder.Register((c, p) => this.app.SystemSettings).ExternallyOwned();
            builder.Register((c, p) => this.app.ErrorReporter).As<ErrorReporter>().ExternallyOwned();
            builder.Register((c, p) => this.app.Log).As<ILog>().ExternallyOwned();

            // Single instance
            builder.RegisterType<Hasher>().SingleInstance();
            builder.RegisterType<FeedContentProvider>().As<IFeedContentProvider>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
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
                    await app.Initialize();

                    while (true)
                    {
                        await app.RunCycle();
                    }
                }
                catch (Exception exception)
                {
                    if (app.Log != null)
                    {
                        await app.ErrorReporter.Error(exception);
                    }
                    else
                    {
                        await Console.Error.WriteLineAsync(exception.ToString());
                    }

                    return 1;
                }
            }
        }
    }
}
