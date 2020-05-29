namespace Newsgirl.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Logging;
    using Shared.Logging.Consumers;

    public class HttpServerApp : IAsyncDisposable
    {
        // ReSharper disable once InconsistentNaming
        private readonly string AppVersion = typeof(HttpServerApp).Assembly.GetName().Version?.ToString();

        private TaskCompletionSource<object> shutdownCompletionSource;
        
        private readonly ManualResetEventSlim shutdownCompleted = new ManualResetEventSlim();

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public HttpServerAppConfig AppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public StructuredLogger Log { get; set; }
        
        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        private CustomHttpServer Server { get; set; }

        public AsyncLocals AsyncLocals { get; set; }
        
        public RpcEngine RpcEngine { get; set; }
        
        public async Task Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskSchedulerOnUnobservedTaskException;
            
            this.AsyncLocals = new AsyncLocalsImpl();
            
            await this.LoadConfig();
            
            var loggerBuilder = new StructuredLoggerBuilder();
            
            loggerBuilder.AddConfig(GeneralLoggingExtensions.GeneralKey, new Dictionary<string, Func<LogConsumer<LogData>>>
            {
                {"ConsoleConsumer", () => new ConsoleLogDataConsumer(this.ErrorReporter)},
                {"ElasticsearchConsumer", () => new ElasticsearchLogDataConsumer(
                    this.ErrorReporter,
                    this.AppConfig.Logging.Elasticsearch,
                    this.AppConfig.Logging.ElasticsearchIndexes.GeneralLogIndex
                )},
            });
                
            loggerBuilder.AddConfig(HttpLoggingExtensions.HttpKey, new Dictionary<string, Func<LogConsumer<HttpLogData>>>
            {
                {"ElasticsearchConsumer", () => new ElasticsearchConsumer<HttpLogData>(
                    this.ErrorReporter,
                    this.AppConfig.Logging.Elasticsearch,
                    this.AppConfig.Logging.ElasticsearchIndexes.HttpLogIndex
                )},
            });
                
            loggerBuilder.AddConfig(HttpLoggingExtensions.HttpDetailedKey, new Dictionary<string, Func<LogConsumer<HttpLogData>>>
            {
                {"ElasticsearchConsumer", () => new ElasticsearchConsumer<HttpLogData>(
                    this.ErrorReporter,
                    this.AppConfig.Logging.Elasticsearch,
                    this.AppConfig.Logging.ElasticsearchIndexes.HttpLogIndex
                )},
            });
            
            this.Log = loggerBuilder.Build();
            
            await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);

            this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, this.ReloadStartupConfig);

            var potentialRpcTypes = typeof(HttpServerApp).Assembly.GetTypes();
            var rpcEngineOptions = new RpcEngineOptions
            {
                PotentialHandlerTypes = potentialRpcTypes
            };
            this.RpcEngine = new RpcEngine(rpcEngineOptions);

            var builder = new ContainerBuilder();
            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new HttpServerIoCModule(this));
            this.IoC = builder.Build();

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }
        
        private async Task LoadConfig()
        {
            this.AppConfig = JsonConvert.DeserializeObject<HttpServerAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            this.AppConfig.ErrorReporter.Release = this.AppVersion;

            // If ErrorReporter is not ErrorReporterImpl - do not replace it. Done for testing purposes.
            if (this.ErrorReporter == null || this.ErrorReporter is ErrorReporterImpl)
            {
                var errorReporter = new ErrorReporterImpl(this.AppConfig.ErrorReporter);
                errorReporter.AddSyncHook(this.AsyncLocals.CollectHttpData);
                this.ErrorReporter = errorReporter;
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
            try
            {
                this.AppConfigWatcher?.Dispose();
                this.AppConfigWatcher = null;

                this.Server?.DisposeAsync();
                this.Server = null;

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
            finally
            {
                this.shutdownCompleted.Set();
            }
        }
      
        public async Task Start(string listenOnAddress = null)
        {
            var serverConfig = new CustomHttpServerConfig
            {
                Addresses = new[]
                {
                    string.IsNullOrWhiteSpace(listenOnAddress) ? "http://127.0.0.1:5000" : listenOnAddress
                }
            };

            async Task RequestDelegate(HttpContext context)
            {
                await using (var requestScope = this.IoC.BeginLifetimeScope())
                {
                    var handler = requestScope.Resolve<RpcRequestHandler>();
                    await handler.HandleRequest(context);
                }
            }

            this.Server = new CustomHttpServerImpl(serverConfig, RequestDelegate);
            
            this.Server.Started += addresses => this.Log.General(() => new LogData($"HTTP server is UP on {string.Join("; ", addresses)} ..."));
            this.Server.Stopping += () => this.Log.General(() => new LogData("HTTP server is shutting down ..."));
            this.Server.Stopped += () => this.Log.General(() => new LogData("HTTP server is down ..."));

            await this.Server.Start();

            this.shutdownCompletionSource = new TaskCompletionSource<object>();
        }

        public async Task Stop()
        {
            await this.Server.Stop();
        }

        public Task AwaitShutdownTrigger()
        {
            return this.shutdownCompletionSource.Task;
        }

        public void TriggerShutdown()
        {
            this.shutdownCompletionSource.SetResult(null);
        }
        
        public void WaitForShutdownToComplete()
        {
            this.shutdownCompleted.Wait();
        }

        public string GetAddress()
        {
            return this.Server.FirstAddress;
        }
    }

    public class HttpServerAppConfig
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
        
        public string HttpLogIndex { get; set; }
    }

    public interface AsyncLocals
    {
        public AsyncLocal<Func<Dictionary<string, object>>> CollectHttpData { get; }
    }

    public class AsyncLocalsImpl : AsyncLocals
    {
        public AsyncLocal<Func<Dictionary<string, object>>> CollectHttpData { get; } = new AsyncLocal<Func<Dictionary<string, object>>>();
    }

    public class HttpServerIoCModule : Module
    {
        private readonly HttpServerApp app;

        public HttpServerIoCModule(HttpServerApp app)
        {
            this.app = app;
        }

        protected override void Load(ContainerBuilder builder)
        {
            // Globally managed
            builder.Register((c, p) => this.app.SystemSettings).ExternallyOwned();
            builder.Register((c, p) => this.app.ErrorReporter).As<ErrorReporter>().ExternallyOwned();
            builder.Register((c, p) => this.app.Log).ExternallyOwned().As<ILog>();
            
            builder.Register((c, p) => this.app.AsyncLocals).ExternallyOwned();
            builder.Register((c, p) => this.app.RpcEngine).ExternallyOwned();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().InstancePerLifetimeScope();

            builder.RegisterType<RpcRequestHandler>().InstancePerLifetimeScope();
            builder.RegisterType<LifetimeScopeInstanceProvider>().As<InstanceProvider>().InstancePerLifetimeScope();

            var handlerClasses = this.app.RpcEngine.Metadata.Select(x => x.HandlerClass).Distinct();
            
            foreach (var handlerClass in handlerClasses)
            {
                builder.RegisterType(handlerClass);
            }
 
            base.Load(builder);
        }
    }

    public static class Program
    {
        private static async Task<int> Main()
        {
            await using (var app = new HttpServerApp())
            {
                try
                {
                    await app.Initialize();

                    await app.Start();

                    AppDomain.CurrentDomain.ProcessExit += (sender, args) =>
                    {
                        app.TriggerShutdown();
                        app.WaitForShutdownToComplete();
                    };

                    Console.CancelKeyPress += (sender, args) =>
                    {
                        app.TriggerShutdown();
                        app.WaitForShutdownToComplete();
                    };

                    await app.AwaitShutdownTrigger();

                    await app.Stop();

                    return 0;
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
