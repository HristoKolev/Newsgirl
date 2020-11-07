namespace Newsgirl.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.ObjectPool;
    using Newtonsoft.Json;
    using Shared;
    using Shared.Logging;
    using Shared.Postgres;

    public class HttpServerApp : IAsyncDisposable
    {
        // ReSharper disable once InconsistentNaming
        private readonly string AppVersion = typeof(HttpServerApp).Assembly.GetName().Version?.ToString();

        private TaskCompletionSource<object> shutdownTriggered;
        private ManualResetEventSlim shutdownCompleted;

        public static readonly RpcEngineOptions RpcEngineOptions = new RpcEngineOptions
        {
            PotentialHandlerTypes = typeof(HttpServerApp).Assembly.GetTypes(),
            //MiddlewareTypes = new[] {typeof(AuthenticationMiddleware)},
            ParameterTypeWhitelist = new[] {typeof(AuthResult)},
        };

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public HttpServerAppConfig AppConfig { get; set; }

        public HttpServerAppConfig InjectedAppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public SystemPools SystemPools { get; set; }

        public StructuredLogger Log { get; set; }

        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        public CustomHttpServer Server { get; set; }

        public AsyncLocals AsyncLocals { get; set; }

        public RpcEngine RpcEngine { get; set; }

        public bool Started { get; set; }

        public Module InjectedIoCModule { get; set; }

        public async Task Start(params string[] listenOnAddresses)
        {
            if (this.Started)
            {
                throw new ApplicationException("The application is already started.");
            }

            this.shutdownTriggered = new TaskCompletionSource<object>();
            this.shutdownCompleted = new ManualResetEventSlim();

            this.AsyncLocals = new AsyncLocalsImpl();

            await this.LoadConfig();

            if (this.InjectedAppConfig == null)
            {
                this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, () => this.ReloadStartupConfig().GetAwaiter().GetResult());
            }

            this.RpcEngine = new RpcEngine(RpcEngineOptions);

            var builder = new ContainerBuilder();
            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new HttpServerIoCModule(this));

            if (this.InjectedIoCModule != null)
            {
                builder.RegisterModule(this.InjectedIoCModule);
            }

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

            loggerBuilder.AddEventStream(HttpLoggingExtensions.HTTP_KEY, new Dictionary<string, Func<EventDestination<HttpLogData>>>
            {
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination<HttpLogData>(
                        this.ErrorReporter,
                        this.AppConfig.Logging.Elasticsearch,
                        this.AppConfig.Logging.ElasticsearchIndexes.HttpLogIndex
                    )
                },
            });

            loggerBuilder.AddEventStream(HttpLoggingExtensions.HTTP_DETAILED_KEY, new Dictionary<string, Func<EventDestination<HttpLogData>>>
            {
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination<HttpLogData>(
                        this.ErrorReporter,
                        this.AppConfig.Logging.Elasticsearch,
                        this.AppConfig.Logging.ElasticsearchIndexes.HttpLogIndex
                    )
                },
            });

            this.Log = loggerBuilder.Build();

            await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
            this.SystemPools = new SystemPoolsImpl(this.SystemSettings);

            this.Server = new CustomHttpServerImpl();
            this.Server.Started += addresses => this.Log.General(() => $"HTTP server is UP on {string.Join("; ", addresses)} ...");
            this.Server.Stopping += () => this.Log.General(() => "HTTP server is shutting down ...");
            this.Server.Stopped += () => this.Log.General(() => "HTTP server is down ...");

            await this.Server.Start(this.ProcessHttpRequest, listenOnAddresses);

            this.Started = true;
        }

        private async Task ProcessHttpRequest(HttpContext context)
        {
            var requestPath = context.Request.Path;

            await using (var requestScope = this.IoC.BeginLifetimeScope())
            {
                var httpContextProvider = requestScope.Resolve<HttpContextProvider>();
                
                // Set the context first before resolving anything else.
                httpContextProvider.HttpContext = context;

                var authFilter = requestScope.Resolve<AuthenticationFilter>();
                var authResult = await authFilter.Authenticate(context.Request.Headers);

                httpContextProvider.AuthResult = authResult;

                const string RPC_ROUTE_PATH = "/rpc";
                if (requestPath.StartsWithSegments(RPC_ROUTE_PATH) && requestPath.Value.Length > RPC_ROUTE_PATH.Length + 1)
                {
                    string rpcRequestType = requestPath.Value.Remove(0, RPC_ROUTE_PATH.Length + 1);
                    var handler = requestScope.Resolve<RpcRequestHandler>();
                    await handler.HandleRequest(context, rpcRequestType);
                    return;
                }

                context.Response.StatusCode = 404;
            }
        }

        private async Task LoadConfig()
        {
            if (this.InjectedAppConfig == null)
            {
                this.AppConfig = JsonConvert.DeserializeObject<HttpServerAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            }
            else
            {
                this.AppConfig = this.InjectedAppConfig;
            }

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
                this.Log.General(() => "Reloading config...");
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

                this.AppConfig = null;
                this.SystemSettings = null;
                this.SystemPools = null;

                if (this.Log != null)
                {
                    await this.Log.DisposeAsync();
                    this.Log = null;
                }

                if (this.IoC != null)
                {
                    await this.IoC.DisposeAsync();
                    this.IoC = null;
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

    public class LoggingConfig
    {
        public EventStreamConfig[] StructuredLogger { get; set; }

        public ElasticsearchConfig Elasticsearch { get; set; }

        public ElasticsearchIndexConfig ElasticsearchIndexes { get; set; }
    }

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

    public interface SystemPools
    {
        ObjectPool<X509Certificate2> JwtSigningCertificates { get; }
    }

    public class SystemPoolsImpl : SystemPools
    {
        public SystemPoolsImpl(SystemSettingsModel systemSettings)
        {
            var factory = new FunctionFactoryObjectPolicy<X509Certificate2>(
                () => new X509Certificate2(systemSettings.SessionCertificate)
            );

            this.JwtSigningCertificates = new DefaultObjectPool<X509Certificate2>(factory, 128);
        }

        public ObjectPool<X509Certificate2> JwtSigningCertificates { get; }
    }

    public class HttpContextProvider
    {
        public HttpContext HttpContext { get; set; }
        
        public AuthResult AuthResult { get; set; }
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
            builder.Register((c, p) => this.app.SystemPools).ExternallyOwned();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();
            builder.RegisterType<AuthService>().InstancePerLifetimeScope();
            builder.RegisterType<JwtServiceImpl>().As<JwtService>().InstancePerLifetimeScope();
            builder.RegisterType<RpcRequestHandler>().InstancePerLifetimeScope();
            builder.RegisterType<LifetimeScopeInstanceProvider>().As<InstanceProvider>().InstancePerLifetimeScope();
            builder.RegisterType<AuthenticationFilter>().InstancePerLifetimeScope();
            builder.RegisterType<HttpContextProvider>().InstancePerLifetimeScope();

            // Always create
            var handlerClasses = this.app.RpcEngine.Metadata.Select(x => x.DeclaringType).Distinct();

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
