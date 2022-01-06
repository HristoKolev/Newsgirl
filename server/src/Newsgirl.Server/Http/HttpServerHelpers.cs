namespace Newsgirl.Server.Http
{
    using System;
    using System.Buffers;
    using System.Linq;
    using System.Security.Cryptography.X509Certificates;
    using Microsoft.AspNetCore.Http;
    using Microsoft.Extensions.ObjectPool;
    using Shared;
    using Shared.Logging;

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

    public static class HttpContextAppExtensions
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

    public class HttpLogData : AppInfoEventData
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
            this.RequestDurationMs = (long)(httpRequestState.RequestEnd - httpRequestState.RequestStart).TotalMilliseconds;

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

        public long RequestDurationMs { get; set; }

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

    public class HttpServerAppConfig
    {
        public string ConnectionString { get; set; }

        public string SentryDsn { get; set; }

        public string InstanceName { get; set; }

        public string Environment { get; set; }

        /// <summary>
        /// The pfx certificate that is used to create JWT tokens.
        /// </summary>
        public string SessionCertificate { get; set; }

        public HttpServerAppLoggingConfig Logging { get; set; }
    }

    public class HttpServerAppLoggingConfig
    {
        public EventStreamConfig[] StructuredLogger { get; set; }

        public ElasticsearchConfig Elasticsearch { get; set; }

        public HttpServerAppElkIndexConfig ElkIndexes { get; set; }
    }

    public class HttpServerAppElkIndexConfig
    {
        public string GeneralLogIndex { get; set; }

        public string HttpLogIndex { get; set; }
    }

    public class SessionCertificatePool : DefaultObjectPool<X509Certificate2>
    {
        private const int MAXIMUM_RETAINED = 128;

        public SessionCertificatePool(HttpServerAppConfig appConfig) :
            base(new SessionCertificatePoolPolicy(appConfig.SessionCertificate), MAXIMUM_RETAINED) { }

        private class SessionCertificatePoolPolicy : DefaultPooledObjectPolicy<X509Certificate2>
        {
            public SessionCertificatePoolPolicy(string certificateBase64)
            {
                this.certificateBytes = Convert.FromBase64String(certificateBase64);
            }

            private readonly byte[] certificateBytes;

            public override X509Certificate2 Create()
            {
                return new X509Certificate2(this.certificateBytes);
            }
        }
    }
}
