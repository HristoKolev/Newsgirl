namespace Newsgirl.Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Shared;
    using Shared.Logging;
    using Shared.Postgres;

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

        public FetcherAppConfig InjectedAppConfig { get; set; }

        public async Task Initialize()
        {
            await this.LoadConfig();

            if (this.InjectedAppConfig == null)
            {
                this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, () => this.ReloadStartupConfig().GetAwaiter().GetResult());
            }

            var builder = new ContainerBuilder();
            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new FetcherIoCModule(this));
            this.IoC = builder.Build();

            var loggerBuilder = new StructuredLoggerBuilder();

            loggerBuilder.AddEventStream(GeneralLoggingExtensions.GENERAL_EVENT_STREAM, new Dictionary<string, Func<EventDestination<LogData>>>
            {
                {"ConsoleConsumer", () => new ConsoleEventDestination(this.ErrorReporter)},
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination(
                        this.ErrorReporter,
                        this.IoC.Resolve<DateTimeService>(),
                        this.AppConfig.Logging.Elasticsearch,
                        this.AppConfig.Logging.ElasticsearchIndexes.GeneralLogIndex
                    )
                },
            });

            this.Log = loggerBuilder.Build();

            await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }

        private async Task LoadConfig()
        {
            if (this.InjectedAppConfig == null)
            {
                this.AppConfig = JsonHelper.Deserialize<FetcherAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            }
            else
            {
                this.AppConfig = this.InjectedAppConfig;
            }

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
                this.Log.General(() => "Reloading config...");
                await this.LoadConfig();
                await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);
            }
            catch (Exception exception)
            {
                await this.ErrorReporter.Error(exception, "FAILED_TO_READ_JSON_CONFIG");
            }
        }

        public async ValueTask DisposeAsync()
        {
            this.AppConfigWatcher?.Dispose();
            this.AppConfigWatcher = null;

            await this.Log.DisposeAsync();
            this.Log = null;

            if (this.IoC != null)
            {
                await this.IoC.DisposeAsync();
                this.IoC = null;
            }

            this.AppConfig = null;
            this.SystemSettings = null;

            // ReSharper disable once SuspiciousTypeConversion.Global
            if (this.ErrorReporter is IAsyncDisposable disposableErrorReporter)
            {
                await disposableErrorReporter.DisposeAsync();
            }

            this.ErrorReporter = null;
        }

        public async Task RunCycle()
        {
            await using (var subContainer = this.IoC.BeginLifetimeScope())
            {
                var fetcherInstance = subContainer.Resolve<FeedFetcher>();

                await fetcherInstance.FetchFeeds();

                this.Log.General(() => $"Waiting {this.SystemSettings.FetcherCyclePause} seconds...");

                await Task.Delay(TimeSpan.FromSeconds(this.SystemSettings.FetcherCyclePause));
            }
        }
    }

    public class FetcherAppConfig
    {
        public string ConnectionString { get; set; }

        public ErrorReporterConfig ErrorReporter { get; set; }

        public LoggingConfig Logging { get; set; }
    }

    public class LoggingConfig
    {
        public EventStreamConfig[] StructuredLogger { get; set; }

        public ElasticsearchConfig Elasticsearch { get; set; }

        public ElasticsearchIndexConfig ElasticsearchIndexes { get; set; }
    }

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
            builder.Register((c, p) => this.app.Log).As<Log>().ExternallyOwned();

            // Single instance
            builder.RegisterType<Hasher>().SingleInstance();
            builder.RegisterType<FeedContentProvider>().As<IFeedContentProvider>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();

            builder.RegisterType<FeedItemsImportService>().As<IFeedItemsImportService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedFetcher>().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }

    public static class Program
    {
        private static async Task<int> Main()
        {
            var app = new FetcherApp();

            async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
            {
                await app.ErrorReporter.Error(e.Exception?.InnerException);
            }

            async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                await app.ErrorReporter.Error((Exception) e.ExceptionObject);
            }

            try
            {
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

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
            finally
            {
                await app.DisposeAsync();

                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }
        }
    }
}
