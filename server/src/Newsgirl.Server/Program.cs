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

        private TaskCompletionSource<object> shutdownTriggered;
        private ManualResetEventSlim shutdownCompleted;
        
        public string AppConfigPath { get; set; }

        public HttpServerAppConfig AppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public StructuredLogger Log { get; set; }
        
        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        private CustomHttpServer Server { get; set; }

        public AsyncLocals AsyncLocals { get; set; }
        
        public RpcEngine RpcEngine { get; set; }

        public bool Started { get; set; }

        public async Task Start(params string[] listenOnAddresses)
        {
            if (this.Started)
            {
                throw new ApplicationException("The application is already started.");
            }

            this.AppConfigPath = EnvVariableHelper.Get("APP_CONFIG_PATH");
            
            this.shutdownTriggered = new TaskCompletionSource<object>();
            this.shutdownCompleted = new ManualResetEventSlim();
            
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

            this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, () => this.ReloadStartupConfig().GetAwaiter().GetResult());

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
            
            async Task RequestDelegate(HttpContext context)
            {
                await using (var requestScope = this.IoC.BeginLifetimeScope())
                {
                    var handler = requestScope.Resolve<RpcRequestHandler>();
                    await handler.HandleRequest(context);
                }
            }

            this.Server = new CustomHttpServerImpl(RequestDelegate);
            
            this.Server.Started += addresses => this.Log.General(() => new LogData($"HTTP server is UP on {string.Join("; ", addresses)} ..."));
            this.Server.Stopping += () => this.Log.General(() => new LogData("HTTP server is shutting down ..."));
            this.Server.Stopped += () => this.Log.General(() => new LogData("HTTP server is down ..."));

            await this.Server.Start(new HttpServerConfig
            {
                Addresses = listenOnAddresses
            });

            this.Started = true;
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

        public async ValueTask Stop()
        {
            if (!this.Started)
            {
                throw new ApplicationException("The application is already stopped.");
            }
            
            try
            {
                this.AppConfigPath = null;

                this.shutdownTriggered = null;
                
                if (this.AppConfigWatcher != null)
                {
                    this.AppConfigWatcher.Dispose();
                    this.AppConfigWatcher = null;    
                }

                if (this.Server != null)
                {
                    await this.Server.Stop();
                    await this.Server.DisposeAsync();
                    this.Server = null;    
                }

                this.RpcEngine = null;

                if (this.IoC != null)
                {
                    await this.IoC.DisposeAsync();
                    this.IoC = null;
                }

                this.AppConfig = null;
                this.SystemSettings = null;

                if (this.Log != null)
                {
                    await this.Log.DisposeAsync();
                    this.Log = null;    
                }
                
                if (this.ErrorReporter != null)
                {
                    // ReSharper disable once SuspiciousTypeConversion.Global
                    if (this.ErrorReporter is IAsyncDisposable disposableErrorReporter)
                    {
                        await disposableErrorReporter.DisposeAsync();
                    }

                    this.ErrorReporter = null;    
                }
                
                this.AsyncLocals = null;
            }
            finally
            {
                if (this.shutdownCompleted != null)
                {
                    this.shutdownCompleted.Set();
                    this.shutdownCompleted = null;    
                }

                this.Started = false;
            }
        }

        public ValueTask DisposeAsync()
        {
            if (this.Started)
            {
                return this.Stop();    
            }

            return new ValueTask();
        }

        public Task AwaitShutdownTrigger()
        {
            return this.shutdownTriggered.Task;
        }

        public void TriggerShutdown()
        {
            this.shutdownTriggered.SetResult(null);
        }
        
        public void WaitForShutdownToComplete()
        {
            this.shutdownCompleted.Wait();
        }

        public string GetAddress()
        {
            return this.Server.BoundAddresses.First();
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
            var app = new HttpServerApp();
            
            async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
            {
                await app.ErrorReporter.Error(e.Exception?.InnerException);
            }

            async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
            {
                await app.ErrorReporter.Error((Exception) e.ExceptionObject);
            }
            
            void OnProcessExit(object sender, EventArgs args)
            {
                if (app.Started)
                {
                    app.TriggerShutdown();
                    app.WaitForShutdownToComplete();
                }
            }

            void OnCancelKeyPress(object sender, ConsoleCancelEventArgs args)
            {
                if (app.Started)
                {
                    args.Cancel = true;
                    app.TriggerShutdown();
                    app.WaitForShutdownToComplete();
                }
            }

            try
            {
                TaskScheduler.UnobservedTaskException += OnUnobservedTaskException;
                AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
                AppDomain.CurrentDomain.ProcessExit += OnProcessExit;
                Console.CancelKeyPress += OnCancelKeyPress;
                    
                await app.Start("http://127.0.0.1:5000");

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
            finally
            {
                await app.DisposeAsync();
                
                Console.CancelKeyPress -= OnCancelKeyPress;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }
        }
    }
}
