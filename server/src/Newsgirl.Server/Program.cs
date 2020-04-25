namespace Newsgirl.Server
{
    using System;
    using System.IO;
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

        private TaskCompletionSource<bool> shutdownCompletionSource;

        public string AppConfigPath => EnvVariableHelper.Get("APP_CONFIG_PATH");

        public HttpServerAppConfig AppConfig { get; set; }

        public SystemSettingsModel SystemSettings { get; set; }

        public ILog Log { get; set; }

        private FileWatcher AppConfigWatcher { get; set; }

        public IContainer IoC { get; set; }

        private HttpServer Server { get; set; }

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

            this.Log = new CustomLogger(this.AppConfig.Logging);
        }

        public async Task Initialize()
        {
            await this.LoadConfig();

            this.AppConfigWatcher = new FileWatcher(this.AppConfigPath, this.ReloadStartupConfig);

            var builder = new ContainerBuilder();

            builder.RegisterModule<SharedModule>();
            builder.RegisterModule(new HttpServerIoCModule(this));

            this.IoC = builder.Build();

            var systemSettingsService = this.IoC.Resolve<SystemSettingsService>();

            this.SystemSettings = await systemSettingsService.ReadSettings<SystemSettingsModel>();
        }

        public async Task Run()
        {
            var serverConfig = new HttpServerConfig
            {
                Addresses = new[] {"http://127.0.0.1:5000"}
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

            this.shutdownCompletionSource = new TaskCompletionSource<bool>();
        }

        public Task WaitForShutdown()
        {
            return this.shutdownCompletionSource.Task;
        }
    }

    public class HttpServerAppConfig
    {
        public string ConnectionString { get; set; }

        public CustomLoggerConfig Logging { get; set; }
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

            // Per scope
            builder.Register((c, p) =>
                    DbFactory.CreateConnection(this.app.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();
            builder.RegisterType<DbService>().InstancePerLifetimeScope();

            builder.Register((ctx, p) =>
            {
                var potentialRpcTypes = typeof(HttpServerApp).Assembly.GetTypes();

                var rpcEngineOptions = new RpcEngineOptions
                {
                    PotentialHandlerTypes = potentialRpcTypes
                };

                return new RpcEngine(rpcEngineOptions, ctx.Resolve<ILog>());
            });

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

                    await app.Run();

                    await app.WaitForShutdown();

                    return 0;
                }
                catch (Exception exception)
                {
                    if (app.Log != null)
                    {
                        await app.Log.Error(exception);
                    }

                    return 1;
                }
            }
        }
    }
}
