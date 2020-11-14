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
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Runtime.ExceptionServices;
    using System.Threading.Tasks;
    using Autofac;
    using Http;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Testing;

    public class HttpServerAppTest : AppDatabaseTest
    {
        private HttpServerAppTester tester;

        protected HttpServerApp App => this.tester.App;

        protected TestRpcClient RpcClient { get; private set; }

        protected const string TEST_EMAIL = "test@abc.de";

        protected const string TEST_PASSWORD = "password123";

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var mockModule = new FunctionAutofacModule(this.ConfigureMocks);

            this.tester = await HttpServerAppTester.Create(this.ConnectionString, mockModule);
            this.RpcClient = new TestRpcClient(this.tester.App.Server.BoundAddresses.First());
        }

        public override async Task DisposeAsync()
        {
            await this.tester.DisposeAsync();
            await base.DisposeAsync();
        }

        protected virtual void ConfigureMocks(ContainerBuilder builder) { }

        protected async Task<RegisterResponse> CreateProfile()
        {
            var registerResult = await this.RpcClient.Register(new RegisterRequest
            {
                Email = TEST_EMAIL,
                Password = TEST_PASSWORD,
            });

            if (!registerResult.IsOk)
            {
                throw new DetailedLogException("The `RegisterRequest` call failed.");
            }

            return registerResult.Payload;
        }
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
            Assert.Null(app.SessionCertificatePool);
            Assert.False(app.Started);

            app.ErrorReporter = new ErrorReporterMock();

            string appConfigPath = Path.GetFullPath("../../../newsgirl-server-test-config.json");
            var injectedConfig = JsonHelper.Deserialize<HttpServerAppConfig>(await File.ReadAllTextAsync(appConfigPath));
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
            Assert.NotNull(app.SessionCertificatePool);
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
            Assert.Null(this.App.SessionCertificatePool);
            Assert.False(this.App.Started);

            TaskScheduler.UnobservedTaskException -= this.OnUnobservedTaskException;
            AppDomain.CurrentDomain.UnhandledException -= this.OnUnhandledException;
        }
    }

    public class TestRpcClient : RpcClient
    {
        private const string CSRF_TOKEN_HEADER = "Csrf-Token";

        private readonly HttpClient httpClient;
        private readonly HttpClientHandler httpClientHandler;

        public Dictionary<string, string> Cookies =>
            this.httpClientHandler.CookieContainer.GetCookies(this.httpClient.BaseAddress)
                .ToDictionary(x => x.Name, x => x.Value);

        public string CsrfToken { get; set; }

        public TestRpcClient(string address)
        {
            this.httpClientHandler = new HttpClientHandler();

            this.httpClient = new HttpClient(this.httpClientHandler)
            {
                BaseAddress = new Uri(address),
            };
        }

        protected override async Task<Result<TResponse>> RpcExecute<TRequest, TResponse>(TRequest request)
        {
            var response = await this.httpClient.SendAsync(new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                Headers = {{CSRF_TOKEN_HEADER, this.CsrfToken}},
                RequestUri = new Uri("/rpc/" + request.GetType().Name, UriKind.Relative),
                Content = new StringContent(
                    JsonHelper.Serialize(request),
                    EncodingHelper.UTF8,
                    "application/json"
                ),
            });

            response.EnsureSuccessStatusCode();

            this.SecureCookiesWorkaround(response.Headers);

            string responseBody = await response.Content.ReadAsStringAsync();

            var result = JsonHelper.Deserialize<Result<TResponse>>(responseBody);

            if (result.Payload is LoginResponse loginResponse)
            {
                this.CsrfToken = loginResponse.CsrfToken;
            }

            if (result.Payload is RegisterResponse registerResponse)
            {
                this.CsrfToken = registerResponse.CsrfToken;
            }

            return result;
        }

        private void SecureCookiesWorkaround(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers)
        {
            var cookies = ParseSetCookieHeaders(headers, this.httpClient.BaseAddress);

            foreach (var cookie in cookies.Where(x => x.Secure))
            {
                cookie.Secure = false;
                this.httpClientHandler.CookieContainer.Add(cookie);
            }
        }

        private static List<Cookie> ParseSetCookieHeaders(IEnumerable<KeyValuePair<string, IEnumerable<string>>> headers, Uri requestUri)
        {
            var cookieValues = new List<Cookie>();

            var setCookieHeaderValues = headers
                .Where(x => string.Equals(x.Key, "Set-Cookie", StringComparison.CurrentCultureIgnoreCase))
                .SelectMany(x => x.Value)
                .ToList();

            foreach (string cookieHeaderValue in setCookieHeaderValues)
            {
                var cookieParts = cookieHeaderValue.Split(';').Select(x => x.Trim()).ToList();

                if (cookieParts.Count == 0)
                {
                    throw new DetailedLogException("Could not parse cookies.")
                    {
                        Details =
                        {
                            {"headers", headers},
                        },
                    };
                }

                var cookie = new Cookie
                {
                    Domain = requestUri.Host,
                };

                for (int i = 0; i < cookieParts.Count; i++)
                {
                    string cookiePart = cookieParts[i];

                    var kvp = cookiePart.Split('=', StringSplitOptions.RemoveEmptyEntries);

                    string key = kvp[0].Trim();
                    string value = kvp.Length > 1 ? kvp[1].Trim() : null;

                    if (i == 0)
                    {
                        cookie.Name = key;
                        cookie.Value = value;
                    }
                    else
                    {
                        switch (key.ToLower())
                        {
                            case "path":
                            {
                                cookie.Path = value;
                                break;
                            }
                            case "secure":
                            {
                                cookie.Secure = true;
                                break;
                            }
                            case "httponly":
                            {
                                cookie.HttpOnly = true;
                                break;
                            }
                            case "expires":
                            {
                                // ReSharper disable once AssignNullToNotNullAttribute
                                cookie.Expires = DateTime.Parse(value);
                                break;
                            }
                        }
                    }
                }

                cookieValues.Add(cookie);
            }

            return cookieValues;
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
