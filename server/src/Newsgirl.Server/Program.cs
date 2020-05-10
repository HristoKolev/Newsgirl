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
    using Shared.Data;
    using Shared.Infrastructure;

    public class HttpServerApp : IAsyncDisposable
    {
        // ReSharper disable once InconsistentNaming
        private readonly string AppVersion = typeof(HttpServerApp).Assembly.GetName().Version?.ToString();

        private TaskCompletionSource<object> shutdownCompletionSource;

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public HttpServerAppConfig AppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public StructuredLogger Log { get; set; }
        
        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        private HttpServer Server { get; set; }

        public AsyncLocals AsyncLocals { get; set; }
        
        public RpcEngine RpcEngine { get; set; }
        
        public async Task Initialize()
        {
            AppDomain.CurrentDomain.UnhandledException += this.CurrentDomainOnUnhandledException;
            TaskScheduler.UnobservedTaskException += this.TaskSchedulerOnUnobservedTaskException;
            
            this.AsyncLocals = new AsyncLocalsImpl();
            
            await this.LoadConfig();
            
            this.Log = new StructuredLogger(builder =>
            {
                builder.AddConfig(GeneralLoggingExtensions.GeneralKey, new LogConsumer<LogData>[]
                {
                    new ConsoleLogDataConsumer(this.ErrorReporter),
                    new ElasticsearchLogDataConsumer(this.ErrorReporter, this.AppConfig.Logging.Elasticsearch, "newsgirl-server-general"), 
                });
                
                builder.AddConfig(HttpLoggingExtensions.HttpKey, new LogConsumer<HttpLogData>[]
                {
                    new ElasticsearchConsumer<HttpLogData>(this.ErrorReporter, this.AppConfig.Logging.Elasticsearch, "newsgirl-server-http"), 
                });
            });
            
            this.Log.SetEnabled(this.AppConfig.Logging.EnabledConfigs);

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

            this.Log?.SetEnabled(this.AppConfig.Logging.EnabledConfigs);
        }

        private async Task ReloadStartupConfig()
        {
            try
            {
                this.Log.General(() => new LogData("Reloading config..."));
                await this.LoadConfig();
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
      
        public async Task Start(string listenOnAddress = null)
        {
            var serverConfig = new HttpServerConfig
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

            this.Server = new HttpServerImpl(this.Log, serverConfig, RequestDelegate);
            
            await this.Server.Start();

            this.shutdownCompletionSource = new TaskCompletionSource<object>();
        }

        public async Task Stop()
        {
            await this.Server.Stop();
        }

        public Task WaitForShutdownTrigger()
        {
            return this.shutdownCompletionSource.Task;
        }

        public void TriggerShutdown()
        {
            this.shutdownCompletionSource.SetResult(null);
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
        public string[] EnabledConfigs { get; set; }
        
        public ElasticsearchConfig Elasticsearch { get; set; }
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

                    Console.CancelKeyPress += (sender, args) =>
                    {
                        app.TriggerShutdown();
                        args.Cancel = true;
                    };
                    
                    await app.WaitForShutdownTrigger();

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
