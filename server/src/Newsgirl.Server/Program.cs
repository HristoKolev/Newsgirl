namespace Newsgirl.Server
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Autofac;
    using Autofac.Core;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Shared.Logging;
    using Shared.Postgres;

    public class HttpServerApp : IAsyncDisposable
    {
        public readonly string AppVersion = typeof(HttpServerApp).Assembly.GetName().Version?.ToString();

        private TaskCompletionSource<object> shutdownTriggered;
        private ManualResetEventSlim shutdownCompleted;

        public static readonly RpcEngineOptions RpcEngineOptions = new RpcEngineOptions
        {
            PotentialHandlerTypes = typeof(HttpServerApp).Assembly.GetTypes(),
            MiddlewareTypes = new[]
            {
                typeof(RpcAuthorizationMiddleware),
                typeof(RpcInputValidationMiddleware),
            },
            ParameterTypeWhitelist = new[]
            {
                typeof(AuthResult),
            },
        };

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public HttpServerAppConfig AppConfig { get; set; }

        public HttpServerAppConfig InjectedAppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public SessionCertificatePool SessionCertificatePool { get; set; }

        public StructuredLogger Log { get; set; }

        public ErrorReporter ErrorReporter { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        public CustomHttpServer Server { get; set; }

        public RpcEngine RpcEngine { get; set; }

        public Module InjectedIoCModule { get; set; }

        public bool Started { get; set; }

        public async Task Start(params string[] listenOnAddresses)
        {
            if (this.Started)
            {
                throw new ApplicationException("The application is already started.");
            }

            this.shutdownTriggered = new TaskCompletionSource<object>();
            this.shutdownCompleted = new ManualResetEventSlim();

            await this.LoadStartupConfig();

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

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();
            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
            this.SessionCertificatePool = new SessionCertificatePool(this.SystemSettings);

            this.Log = await CreateLogger(
                this.SystemSettings.HttpServerAppLoggingConfig,
                this.ErrorReporter,
                this.IoC.Resolve<LogPreprocessor>()
            );

            this.Server = new CustomHttpServerImpl();
            this.Server.Started += addresses => this.Log.General(() => $"HTTP server is UP on {string.Join("; ", addresses)} ...");
            this.Server.Stopping += () => this.Log.General(() => "HTTP server is shutting down ...");
            this.Server.Stopped += () => this.Log.General(() => "HTTP server is down ...");

            await this.Server.Start(this.ProcessHttpRequest, listenOnAddresses);

            this.Started = true;
        }

        private static async Task<StructuredLogger> CreateLogger(
            HttpServerAppLoggingConfig loggingConfig,
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

            builder.AddEventStream(HttpLoggingExtensions.HTTP_KEY, new Dictionary<string, Func<EventDestination<HttpLogData>>>
            {
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination<HttpLogData>(
                        errorReporter,
                        loggingConfig.Elasticsearch,
                        loggingConfig.ElkIndexes.HttpLogIndex
                    )
                },
            });

            builder.AddEventStream(HttpLoggingExtensions.HTTP_DETAILED_KEY, new Dictionary<string, Func<EventDestination<HttpLogData>>>
            {
                {
                    "ElasticsearchConsumer", () => new ElasticsearchEventDestination<HttpLogData>(
                        errorReporter,
                        loggingConfig.Elasticsearch,
                        loggingConfig.ElkIndexes.HttpLogIndex
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
                this.AppConfig = JsonHelper.Deserialize<HttpServerAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
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
                this.SessionCertificatePool = null;

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

        private async Task ProcessHttpRequest(HttpContext context)
        {
            await using (var requestScope = this.IoC.BeginLifetimeScope())
            {
                ErrorReporter scopedErrorReporter = null;

                try
                {
                    scopedErrorReporter = requestScope.Resolve<ErrorReporter>();

                    // Resolve this to set the event timestamp.
                    var dateTimeService = requestScope.Resolve<DateTimeService>();

                    // Use this for request scoped state like the reference to the HttpContext object.
                    var httpRequestState = requestScope.Resolve<HttpRequestState>();

                    scopedErrorReporter.AddDataHook(() => new Dictionary<string, object> {{"httpRequestState", httpRequestState}});

                    // Set the context first before resolving anything else.
                    httpRequestState.HttpContext = context;

                    try
                    {
                        httpRequestState.RequestStart = dateTimeService.EventTime();

                        // Set the result of the authentication step.
                        httpRequestState.AuthResult = await requestScope.Resolve<AuthenticationFilter>()
                            .Authenticate(context.Request.Headers);

                        // The value that we route on.
                        string requestPath = context.Request.Path.Value;

                        const string RPC_ROUTE_PATH = "/rpc/";

                        if (requestPath.StartsWith(RPC_ROUTE_PATH))
                        {
                            httpRequestState.RpcState = new RpcRequestState();

                            if (requestPath.Length < RPC_ROUTE_PATH.Length + 1)
                            {
                                httpRequestState.RpcState.RpcRequestType = null;
                            }
                            else
                            {
                                httpRequestState.RpcState.RpcRequestType = requestPath.Remove(0, RPC_ROUTE_PATH.Length);
                            }

                            await requestScope.Resolve<RpcRequestHandler>().HandleRpcRequest(httpRequestState);

                            return;
                        }

                        // other http endpoints come here

                        // otherwise 404
                        context.Response.StatusCode = 404;
                    }
                    finally
                    {
                        httpRequestState.RequestEnd = dateTimeService.CurrentTime();

                        this.Log.Http(() => new HttpLogData(httpRequestState, false));
                        this.Log.HttpDetailed(() => new HttpLogData(httpRequestState, true));
                    }
                }
                catch (Exception error)
                {
                    var errorReporter = scopedErrorReporter ?? this.ErrorReporter;
                    await errorReporter.Error(error, "GENERAL_HTTP_ERROR");
                    throw;
                }
            }
        }
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
            builder.Register((c, p) => this.app.Log).As<Log>().ExternallyOwned();
            builder.Register((c, p) => this.app.RpcEngine).ExternallyOwned();
            builder.Register((c, p) => this.app.SessionCertificatePool).ExternallyOwned();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();

            builder.Register(this.CreateErrorReporter).InstancePerLifetimeScope();
            builder.Register(this.CreateLogProcessor).InstancePerLifetimeScope();
            builder.RegisterType<AuthServiceImpl>().As<AuthService>().InstancePerLifetimeScope();
            builder.RegisterType<JwtServiceImpl>().As<JwtService>().InstancePerLifetimeScope();
            builder.RegisterType<RpcRequestHandler>().InstancePerLifetimeScope();
            builder.RegisterType<LifetimeScopeInstanceProvider>().As<InstanceProvider>().InstancePerLifetimeScope();
            builder.RegisterType<AuthenticationFilter>().InstancePerLifetimeScope();
            builder.RegisterType<HttpRequestState>().InstancePerLifetimeScope();
            builder.RegisterType<RpcAuthorizationMiddleware>().InstancePerLifetimeScope();
            builder.RegisterType<RpcInputValidationMiddleware>().InstancePerLifetimeScope();

            // Always create
            var handlerClasses = this.app.RpcEngine.Metadata.Select(x => x.DeclaringType).Distinct().ToList();

            foreach (var handlerClass in handlerClasses)
            {
                builder.RegisterType(handlerClass);
            }

            base.Load(builder);
        }

        private ErrorReporter CreateErrorReporter(IComponentContext context, IEnumerable<Parameter> parameters)
        {
            var config = new ErrorReporterImplConfig
            {
                SentryDsn = this.app.AppConfig.SentryDsn,
                Environment = this.app.AppConfig.Environment,
                InstanceName = this.app.AppConfig.InstanceName,
                AppVersion = this.app.AppVersion,
            };

            return new ErrorReporterImpl(config);
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

                Console.CancelKeyPress -= OnCancelKeyPress;
                AppDomain.CurrentDomain.ProcessExit -= OnProcessExit;
                AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
                TaskScheduler.UnobservedTaskException -= OnUnobservedTaskException;
            }
        }
    }
}
