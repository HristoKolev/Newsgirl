namespace Newsgirl.Server
{
    using System;
    using System.Buffers;
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

        public AsyncLocals AsyncLocals { get; set; }

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
            this.SessionCertificatePool = new SessionCertificatePool(this.SystemSettings);

            this.Server = new CustomHttpServerImpl();
            this.Server.Started += addresses => this.Log.General(() => $"HTTP server is UP on {string.Join("; ", addresses)} ...");
            this.Server.Stopping += () => this.Log.General(() => "HTTP server is shutting down ...");
            this.Server.Stopped += () => this.Log.General(() => "HTTP server is down ...");

            await this.Server.Start(this.ProcessHttpRequest, listenOnAddresses);

            this.Started = true;
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

                    // Set the context first before resolving anything else.
                    httpRequestState.HttpContext = context;

                    try
                    {
                        httpRequestState.RequestStart = dateTimeService.EventTime();

                        // Diagnostic data in case of an error.
                        this.AsyncLocals.CollectHttpData.Value = () => new Dictionary<string, object>
                        {
                            {"http", new HttpLogData(httpRequestState, true)},
                        };

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

        private async Task LoadConfig()
        {
            if (this.InjectedAppConfig == null)
            {
                this.AppConfig = JsonHelper.Deserialize<HttpServerAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));
            }
            else
            {
                this.AppConfig = this.InjectedAppConfig;
            }

            this.AppConfig.ErrorReporter.Release = this.AppVersion;

            var errorReporter = new ErrorReporterImpl(this.AppConfig.ErrorReporter);
            errorReporter.AddSyncHook(this.AsyncLocals.CollectHttpData);

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
                await this.LoadConfig();
                await this.Log.Reconfigure(this.AppConfig.Logging.StructuredLogger);
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

    public class SessionCertificatePool : DefaultObjectPool<X509Certificate2>
    {
        private const int MAXIMUM_RETAINED = 128;

        public SessionCertificatePool(SystemSettingsModel systemSettings) :
            base(new SessionCertificatePoolPolicy(systemSettings.SessionCertificate), MAXIMUM_RETAINED) { }

        private class SessionCertificatePoolPolicy : DefaultPooledObjectPolicy<X509Certificate2>
        {
            public SessionCertificatePoolPolicy(byte[] certificateBytes)
            {
                this.certificateBytes = certificateBytes;
            }

            private readonly byte[] certificateBytes;

            public override X509Certificate2 Create()
            {
                return new X509Certificate2(this.certificateBytes);
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
            builder.Register((c, p) => this.app.ErrorReporter).As<ErrorReporter>().ExternallyOwned();
            builder.Register((c, p) => this.app.Log).ExternallyOwned().As<Log>();

            builder.Register((c, p) => this.app.AsyncLocals).ExternallyOwned();
            builder.Register((c, p) => this.app.RpcEngine).ExternallyOwned();
            builder.Register((c, p) => this.app.SessionCertificatePool).ExternallyOwned();

            // Per scope
            builder.Register((c, p) => DbFactory.CreateConnection(this.app.AppConfig.ConnectionString)).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();
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
    }

    public class HttpRequestState : IDisposable
    {
        public HttpContext HttpContext { get; set; }

        public AuthResult AuthResult { get; set; }

        public DateTime RequestStart { get; set; }

        public DateTime RequestEnd { get; set; }

        public RpcRequestState RpcState { get; set; }

        public void Dispose()
        {
            this.RpcState?.Dispose();
        }
    }

    public static class HttpContextExtensions
    {
        public static void AddErrorIdHeader(this HttpContext httpContext, string errorID)
        {
            if (!httpContext.Response.HasStarted)
            {
                httpContext.Response.Headers["errorID"] = errorID;
            }
        }
    }

    public class RpcRequestState : IDisposable
    {
        public string RpcRequestType { get; set; }

        public IMemoryOwner<byte> RpcRequestBody { get; set; }

        public object RpcRequestPayload { get; set; }

        public Result<object> RpcResponse { get; set; }

        public void Dispose()
        {
            this.RpcRequestBody?.Dispose();
        }
    }

    public class HttpLogData
    {
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper disable UnusedAutoPropertyAccessor.Global

        public HttpLogData(HttpRequestState httpRequestState, bool detailedLog)
        {
            var context = httpRequestState.HttpContext;
            var connectionInfo = context.Connection;
            var httpRequest = context.Request;

            this.LocalIp = connectionInfo.LocalIpAddress + ":" + connectionInfo.LocalPort;
            this.RemoteIp = connectionInfo.RemoteIpAddress + ":" + connectionInfo.RemotePort;
            this.HttpRequestID = connectionInfo.Id;
            this.Method = httpRequest.Method;
            this.Path = httpRequest.Path.ToString();
            this.Query = httpRequest.QueryString.ToString();
            this.Protocol = httpRequest.Protocol;
            this.Scheme = httpRequest.Scheme;
            this.Aborted = context.RequestAborted.IsCancellationRequested;
            this.StatusCode = context.Response.StatusCode;

            // --------

            this.RequestStart = httpRequestState.RequestStart;
            this.RequestEnd = httpRequestState.RequestEnd;
            this.RequestDurationMs = (httpRequestState.RequestEnd - httpRequestState.RequestStart).TotalMilliseconds;

            // --------

            if (httpRequestState.AuthResult != null)
            {
                this.SessionID = httpRequestState.AuthResult.SessionID;
                this.LoginID = httpRequestState.AuthResult.LoginID;
                this.ProfileID = httpRequestState.AuthResult.ProfileID;
                this.ValidCsrfToken = httpRequestState.AuthResult.ValidCsrfToken;
            }

            var rpcState = httpRequestState.RpcState;

            if (rpcState != null)
            {
                this.RpcRequestType = rpcState.RpcRequestType;
            }

            if (detailedLog)
            {
                this.HeadersJson = JsonHelper.Serialize(httpRequest.Headers.ToDictionary(x => x.Key, x => x.Value));

                if (rpcState != null)
                {
                    if (rpcState.RpcRequestBody != null)
                    {
                        this.RpcRequestBodyBase64 = Convert.ToBase64String(rpcState.RpcRequestBody.Memory.Span);
                    }

                    if (rpcState.RpcRequestPayload != null)
                    {
                        this.RpcRequestPayloadJson = JsonHelper.Serialize(rpcState.RpcRequestPayload);
                    }

                    if (rpcState.RpcResponse != null)
                    {
                        this.RpcResponseJson = JsonHelper.Serialize(rpcState.RpcResponse);
                    }
                }
            }
        }

        public string LocalIp { get; set; }

        public string RemoteIp { get; set; }

        public string HttpRequestID { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string Query { get; set; }

        public string Protocol { get; set; }

        public string Scheme { get; set; }

        public bool Aborted { get; set; }

        public int StatusCode { get; set; }

        public string HeadersJson { get; set; }

        // --------

        public DateTime RequestStart { get; set; }

        public DateTime RequestEnd { get; set; }

        public double RequestDurationMs { get; set; }

        // --------

        public int SessionID { get; set; }

        public int LoginID { get; set; }

        public int ProfileID { get; set; }

        public bool ValidCsrfToken { get; set; }

        // --------

        public string RpcRequestType { get; set; }

        public string RpcRequestBodyBase64 { get; set; }

        public string RpcRequestPayloadJson { get; set; }

        public string RpcResponseJson { get; set; }

        // ReSharper restore UnusedAutoPropertyAccessor.Global
        // ReSharper restore AutoPropertyCanBeMadeGetOnly.Global
        // ReSharper restore MemberCanBePrivate.Global
    }

    public static class HttpLoggingExtensions
    {
        public const string HTTP_KEY = "HTTP_REQUESTS";
        public const string HTTP_DETAILED_KEY = "HTTP_REQUESTS_DETAILED";

        public static void Http(this Log log, Func<HttpLogData> func)
        {
            log.Log(HTTP_KEY, func);
        }

        public static void HttpDetailed(this Log log, Func<HttpLogData> func)
        {
            log.Log(HTTP_DETAILED_KEY, func);
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
