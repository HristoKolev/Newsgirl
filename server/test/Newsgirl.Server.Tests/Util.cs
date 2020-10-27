using ApprovalTests.Namers;
using ApprovalTests.Reporters;
using Newsgirl.Testing;
using Xunit;

[assembly: UseReporter(typeof(CustomReporter))]
[assembly: UseApprovalSubdirectory("./snapshots")]
[assembly: CollectionBehavior(MaxParallelThreads = 32)]

namespace Newsgirl.Server.Tests
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net.Http;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Autofac;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Newtonsoft.Json;
    using Shared;
    using Testing;

    public class HttpServerAppTest : AppDatabaseTest
    {
        private HttpServerAppTester tester;

        protected HttpServerApp App => this.tester.App;

        protected RpcClient RpcClient { get; private set; }

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var mockModule = new FunctionAutofacModule(this.ConfigureMocks);

            this.tester = await HttpServerAppTester.Create(this.ConnectionString, mockModule);
            this.RpcClient = new TestRpcClient(this.tester.App);
        }

        public override async Task DisposeAsync()
        {
            await this.tester.DisposeAsync();
            await base.DisposeAsync();
        }

        protected virtual void ConfigureMocks(ContainerBuilder builder) { }
    }

    public class FunctionAutofacModule : Module
    {
        private readonly Action<ContainerBuilder> func;

        public FunctionAutofacModule(Action<ContainerBuilder> func)
        {
            this.func = func;
        }

        protected override void Load(ContainerBuilder builder)
        {
            this.func(builder);

            base.Load(builder);
        }
    }

    public class HttpServerAppTester : IAsyncDisposable
    {
        public HttpServerApp App { get; private set; }

        public static async Task<HttpServerAppTester> Create(string connectionString, Module mockModule)
        {
            var tester = new HttpServerAppTester();

            var app = new HttpServerApp
            {
                InjectedIoCModule = mockModule,
            };

            TaskScheduler.UnobservedTaskException += tester.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException += tester.OnUnhandledException;

            Assert.Null(app.Log);
            Assert.Null(app.AppConfig);
            Assert.Null(app.AsyncLocals);
            Assert.Null(app.ErrorReporter);
            Assert.Null(app.IoC);
            Assert.Null(app.RpcEngine);
            Assert.Null(app.SystemSettings);
            Assert.Null(app.SystemPools);
            Assert.False(app.Started);

            app.ErrorReporter = new ErrorReporterMock();

            string appConfigPath = Path.GetFullPath("../../../newsgirl-server-test-config.json");
            var injectedConfig = JsonConvert.DeserializeObject<HttpServerAppConfig>(await File.ReadAllTextAsync(appConfigPath));
            injectedConfig.ConnectionString = connectionString;
            app.InjectedAppConfig = injectedConfig;

            await app.Start("http://127.0.0.1:0");

            Assert.NotNull(app.Log);
            Assert.NotNull(app.AppConfig);
            Assert.NotNull(app.AsyncLocals);
            Assert.NotNull(app.ErrorReporter);
            Assert.NotNull(app.IoC);
            Assert.NotNull(app.RpcEngine);
            Assert.NotNull(app.SystemSettings);
            Assert.NotNull(app.SystemPools);
            Assert.True(app.Started);

            tester.App = app;

            return tester;
        }

        private async void OnUnobservedTaskException(object sender, UnobservedTaskExceptionEventArgs e)
        {
            await this.App.ErrorReporter.Error(e.Exception!.InnerException);
        }

        private async void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            await this.App.ErrorReporter.Error((Exception) e.ExceptionObject);
        }

        public async ValueTask DisposeAsync()
        {
            await this.App.DisposeAsync();

            Assert.Null(this.App.Log);
            Assert.Null(this.App.AppConfig);
            Assert.Null(this.App.AsyncLocals);
            Assert.Null(this.App.ErrorReporter);
            Assert.Null(this.App.IoC);
            Assert.Null(this.App.RpcEngine);
            Assert.Null(this.App.SystemSettings);
            Assert.Null(this.App.SystemPools);
            Assert.False(this.App.Started);

            TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
        }
    }

    public class TestRpcClient : RpcClient
    {
        private readonly HttpServerApp app;

        public TestRpcClient(HttpServerApp app)
        {
            this.app = app;
        }

        protected override async Task<RpcResult<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request)
        {
            var requestMessage = new RpcRequestMessage
            {
                Payload = request,
                Type = request.GetType().Name,
            };

            var instanceProvider = this.app.IoC.Resolve<InstanceProvider>();

            var result = await this.app.RpcEngine.Execute(requestMessage, instanceProvider);

            if (result.IsOk)
            {
                return RpcResult.Ok((TResponse) result.Payload);
            }

            return RpcResult.Error<TResponse>(result.ErrorMessages);
        }
    }

    /// <summary>
    /// A testing service that facilitates testing of <see cref="RequestDelegate" />'s.
    /// Creates <see cref="CustomHttpServerImpl" /> with given <see cref="RequestDelegate" /> and starts it on a random port.
    /// Creates an <see cref="HttpClient" /> with <see cref="HttpClient.BaseAddress" /> that points to the
    /// <see cref="CustomHttpServerImpl" />.
    /// Disposes both <see cref="CustomHttpServerImpl" /> and <see cref="HttpClient" /> when <see cref="DisposeAsync" /> is
    /// called.
    /// </summary>
    public class HttpServerTester : IAsyncDisposable
    {
        private HttpServerTester() { }

        /// <summary>
        /// This gets populated with an <see cref="Exception" /> object
        /// if the given <see cref="RequestDelegate" /> throws.
        /// </summary>
        public Exception Exception { get; set; }

        public CustomHttpServerImpl Server { get; set; }

        public HttpClient Client { get; set; }

        public async ValueTask DisposeAsync()
        {
            await this.Server.DisposeAsync();
            this.Client.Dispose();
        }

        /// <summary>
        /// Creates and starts a new instance of <see cref="HttpServerTester" />
        /// </summary>
        public static async Task<HttpServerTester> Create(RequestDelegate requestDelegate)
        {
            var tester = new HttpServerTester();

            async Task Handler(HttpContext context)
            {
                try
                {
                    await requestDelegate(context);
                }
                catch (Exception err)
                {
                    tester.Exception = err;
                    throw;
                }
            }

            var server = new CustomHttpServerImpl();

            await server.Start(Handler, new[] {"http://127.0.0.1:0"});

            var client = new HttpClient
            {
                BaseAddress = new Uri(server.BoundAddresses.First()),
            };

            tester.Server = server;
            tester.Client = client;

            return tester;
        }

        public void EnsureHandlerSuccess()
        {
            if (this.Exception != null)
            {
                ExceptionDispatchInfo.Capture(this.Exception).Throw();
            }
        }
    }
}
