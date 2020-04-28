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

        public ILog Log { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        private HttpServer Server { get; set; }

        public AsyncLocals AsyncLocals { get; set; }
        
        public RpcEngine RpcEngine { get; set; }

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

            this.Log = null;
        }

        private async Task ReloadStartupConfig()
        {
            try
            {
                this.Log.Log("Reloading config...");

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
                JsonConvert.DeserializeObject<HttpServerAppConfig>(await File.ReadAllTextAsync(this.AppConfigPath));

            this.AppConfig.Logging.Release = this.AppVersion;

            var logger = new CustomLogger(this.AppConfig.Logging);
            logger.AddSyncHook(this.AsyncLocals.CollectHttpData);
            this.Log = logger;
        }

        public async Task Initialize()
        {
            this.AsyncLocals = new AsyncLocalsImpl();
            
            await this.LoadConfig();

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

        public Task WaitForShutdownSignal()
        {
            return this.shutdownCompletionSource.Task;
        }

        public void RequestShutdown()
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

        public CustomLoggerConfig Logging { get; set; }
    }

    public interface AsyncLocals
    {
        public AsyncLocal<Func<Dictionary<string, object>>> CollectHttpData { get; }
    }

    public class AsyncLocalsImpl : AsyncLocals
    {
        public AsyncLocal<Func<Dictionary<string, object>>> CollectHttpData { get; } = new AsyncLocal<Func<Dictionary<string, object>>>();
    }

    public class LifetimeScopeInstanceProvider : InstanceProvider
    {
        private readonly ILifetimeScope lifetimeScope;

        public LifetimeScopeInstanceProvider(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }
        
        public object Get(Type type)
        {
            return this.lifetimeScope.Resolve(type);
        }

        public T Get<T>()
        {
            return this.lifetimeScope.Resolve<T>();
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
            builder.Register((c, p) => this.app.SystemSettings);
            builder.Register((c, p) => this.app.Log);
            builder.Register((c, p) => this.app.AsyncLocals);
            builder.Register((c, p) => this.app.RpcEngine);

            // Per scope
            builder.Register((c, p) =>
                    DbFactory.CreateConnection(this.app.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();
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
                        app.RequestShutdown();
                    };

                    await app.WaitForShutdownSignal();

                    return 0;
                }
                catch (Exception exception)
                {
                    if (app.Log != null)
                    {
                        await app.Log.Error(exception);
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
