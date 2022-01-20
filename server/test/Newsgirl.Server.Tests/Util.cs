namespace Newsgirl.Server.Tests;

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Auth;
using Autofac;
using Infrastructure;
using Xdxd.DotNet.Shared;
using Xdxd.DotNet.Testing;
using Xunit;

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
            throw new DetailedException("The `RegisterRequest` call failed.");
        }

        return registerResult.Payload;
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
        Assert.Null(app.ErrorReporter);
        Assert.Null(app.IoC);
        Assert.Null(app.RpcEngine);
        Assert.Null(app.SessionCertificatePool);
        Assert.False(app.Started);

        app.ErrorReporter = new ErrorReporterMock();

        string appConfigPath = Path.GetFullPath("../../../newsgirl-server.json");
        var injectedConfig = JsonHelper.Deserialize<HttpServerAppConfig>(await File.ReadAllTextAsync(appConfigPath));
        injectedConfig.ConnectionString = connectionString;
        app.InjectedAppConfig = injectedConfig;

        await app.Start("http://127.0.0.1:0");

        Assert.NotNull(app.Log);
        Assert.NotNull(app.AppConfig);
        Assert.NotNull(app.ErrorReporter);
        Assert.NotNull(app.IoC);
        Assert.NotNull(app.RpcEngine);
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
        await this.App.ErrorReporter.Error((Exception)e.ExceptionObject);
    }

    public async ValueTask DisposeAsync()
    {
        await this.App.DisposeAsync();

        Assert.Null(this.App.Log);
        Assert.Null(this.App.AppConfig);
        Assert.Null(this.App.ErrorReporter);
        Assert.Null(this.App.IoC);
        Assert.Null(this.App.RpcEngine);
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
        this.httpClientHandler.CookieContainer.GetCookies(this.httpClient.BaseAddress!)
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
            Headers = { { CSRF_TOKEN_HEADER, this.CsrfToken } },
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

        // ReSharper disable once PossibleMultipleEnumeration
        var setCookieHeaderValues = headers
            .Where(x => string.Equals(x.Key, "Set-Cookie", StringComparison.CurrentCultureIgnoreCase))
            .SelectMany(x => x.Value)
            .ToList();

        foreach (string cookieHeaderValue in setCookieHeaderValues)
        {
            var cookieParts = cookieHeaderValue.Split(';').Select(x => x.Trim()).ToList();

            if (cookieParts.Count == 0)
            {
                throw new DetailedException("Could not parse cookies.")
                {
                    Details =
                    {
                        // ReSharper disable once PossibleMultipleEnumeration
                        { "headers", headers },
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
                    cookie.Value = value!;
                }
                else
                {
                    switch (key.ToLower())
                    {
                        case "path":
                        {
                            cookie.Path = value!;
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

public class PasswordServiceMock : PasswordService
{
    public string HashPassword(string password)
    {
        return $"$${password}$$";
    }

    public bool VerifyPassword(string password, string passwordHash)
    {
        return passwordHash.Remove(passwordHash.Length - 2, 2).Remove(0, 2) == password;
    }
}
