﻿namespace Newsgirl.Fetcher
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Shared;
    using Shared.Logging;
    using Shared.Postgres;

    public class FetcherApp : IAsyncDisposable
    {
        public readonly string AppVersion = typeof(FetcherApp).Assembly.GetName().Version?.ToString();

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public FetcherAppConfig AppConfig { get; set; }

        public FetcherAppConfig InjectedAppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public StructuredLogger Log { get; set; }

        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        public Module InjectedIoCModule { get; set; }

        public async Task Initialize()
        {
            await this.LoadStartupConfig();

            if (this.InjectedAppConfig == null)
            {
                this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, () => this.ReloadStartupConfig().GetAwaiter().GetResult());
            }

            var builder = new ContainerBuilder();
            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new FetcherIoCModule(this));

            if (this.InjectedIoCModule != null)
            {
                builder.RegisterModule(this.InjectedIoCModule);
            }

            this.IoC = builder.Build();

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();

            this.Log = await CreateLogger(
                this.SystemSettings.FetcherAppLoggingConfig,
                this.ErrorReporter,
                this.IoC.Resolve<LogPreprocessor>()
            );
        }

        private static async Task<StructuredLogger> CreateLogger(
            FetcherAppLoggingConfig loggingConfig,
            ErrorReporter errorReporter,
            EventPreprocessor eventPreprocessor)
        {
            var builder = new StructuredLoggerBuilder();

            builder.AddEventStream(GeneralLoggingExtensions.GENERAL_EVENT_STREAM, new Dictionary<string, Func<EventDestination<LogData>>>
            {
                {
                    "ConsoleConsumer", () => new ConsoleEventDestination(errorReporter)
                },
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination(
                        errorReporter,
                        loggingConfig.Elasticsearch,
                        loggingConfig.ElkIndexes.GeneralLogIndex
                    )
                },
            });

            builder.AddEventStream(FetcherRunDataExtensions.FETCHER_EVENT_STREAM, new Dictionary<string, Func<EventDestination<FetcherRunData>>>
            {
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination<FetcherRunData>(
                        errorReporter,
                        loggingConfig.Elasticsearch,
                        loggingConfig.ElkIndexes.FetcherLogIndex
                    )
                },
            });

            var log = builder.Build();

            await log.Reconfigure(loggingConfig.StructuredLogger, eventPreprocessor);

            return log;
        }

        private async Task LoadStartupConfig()
        {
            if (this.InjectedAppConfig == null)
            {
                this.AppConfig = JsonHelper.Deserialize<FetcherAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            }
            else
            {
                this.AppConfig = this.InjectedAppConfig;
            }

            var errorReporter = new ErrorReporterImpl(new ErrorReporterImplConfig
            {
                SentryDsn = this.AppConfig.SentryDsn,
                Environment = this.AppConfig.Environment,
                InstanceName = this.AppConfig.InstanceName,
                AppVersion = this.AppVersion,
            });

            // If ErrorReporter is not ErrorReporterImpl - do not replace it. Done for testing purposes.
            if (this.ErrorReporter == null || this.ErrorReporter is ErrorReporterImpl)
            {
                this.ErrorReporter = errorReporter;
            }
            else
            {
                this.ErrorReporter.SetInnerReporter(errorReporter);
            }
        }

        private async Task ReloadStartupConfig()
        {
            try
            {
                this.Log.General(() => "Reloading config...");
                await this.LoadStartupConfig();
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
            await using (var scope = this.IoC.BeginLifetimeScope())
            {
                var fetcherInstance = scope.Resolve<FeedFetcher>();
                var fetcherRunData = await fetcherInstance.FetchFeeds();

                this.Log.FetcherLog(() => fetcherRunData);

                await Task.Delay(TimeSpan.FromSeconds(this.SystemSettings.FetcherCyclePause));
            }
        }
    }

    public class FetcherRunData
    {
        public DateTime StartTime { get; set; }

        public DateTime EndTime { get; set; }

        public long Duration { get; set; }

        public int FeedCount { get; set; }

        public int ChangedFeedCount { get; set; }

        public int ChangedFeedItemCount { get; set; }
    }

    /// <summary>
    /// Using this extension method allows us to not specify the stream name and the event data structure.
    /// </summary>
    public static class FetcherRunDataExtensions
    {
        public const string FETCHER_EVENT_STREAM = "FETCHER_LOG";

        public static void FetcherLog(this Log log, Func<FetcherRunData> func)
        {
            log.Log(FETCHER_EVENT_STREAM, func);
        }
    }

    public class FetcherAppConfig
    {
        public string ConnectionString { get; set; }

        public string SentryDsn { get; set; }

        public string InstanceName { get; set; }

        public string Environment { get; set; }
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
            builder.RegisterType<FeedContentProvider>().As<IFeedContentProvider>().SingleInstance();
            builder.RegisterType<FeedParser>().As<IFeedParser>().SingleInstance();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedItemsImportService>().As<IFeedItemsImportService>().InstancePerLifetimeScope();
            builder.RegisterType<FeedFetcher>().InstancePerLifetimeScope();
            builder.Register(this.CreateLogProcessor).InstancePerLifetimeScope();

            base.Load(builder);
        }

        private LogPreprocessor CreateLogProcessor(IComponentContext context, IEnumerable<Parameter> parameters)
        {
            var dateTimeService = context.Resolve<DateTimeService>();

            var config = new LogPreprocessorConfig
            {
                Environment = this.app.AppConfig.Environment,
                InstanceName = this.app.AppConfig.InstanceName,
                AppVersion = this.app.AppVersion,
            };

            return new LogPreprocessor(dateTimeService, config);
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
                if (app.ErrorReporter != null)
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
