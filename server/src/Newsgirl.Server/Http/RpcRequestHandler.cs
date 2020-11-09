namespace Newsgirl.Server.Http
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Shared.Logging;

    /// <summary>
    /// Serves Rpc requests over HTTP.
    /// </summary>
    public class RpcRequestHandler
    {
        private static readonly JsonSerializerOptions SerializationOptions = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        };

        private readonly RpcEngine rpcEngine;
        private readonly InstanceProvider instanceProvider;
        private readonly AsyncLocals asyncLocals;
        private readonly ErrorReporter errorReporter;
        private readonly DateTimeService dateTimeService;
        private readonly Log log;

        private HttpContext httpContext;
        private IMemoryOwner<byte> requestBody;
        private RpcRequestMessage rpcRequest;
        private Result<object> rpcResponse;
        private DateTime requestStart;
        private string requestType;

        public RpcRequestHandler(
            RpcEngine rpcEngine,
            InstanceProvider instanceProvider,
            AsyncLocals asyncLocals,
            ErrorReporter errorReporter,
            DateTimeService dateTimeService,
            Log log)
        {
            this.rpcEngine = rpcEngine;
            this.instanceProvider = instanceProvider;
            this.asyncLocals = asyncLocals;
            this.errorReporter = errorReporter;
            this.dateTimeService = dateTimeService;
            this.log = log;
        }

        public async Task HandleRequest(HttpContext context, string rpcRequestType)
        {
            this.httpContext = context;
            this.requestType = rpcRequestType;
            this.requestStart = this.dateTimeService.CurrentTime();

            // Diagnostic data in case of an error.
            this.asyncLocals.CollectHttpData.Value = () => new Dictionary<string, object>
            {
                {
                    "http", new HttpLogData(
                        this.httpContext,
                        // ReSharper disable once AccessToDisposedClosure
                        this.requestBody,
                        this.rpcRequest,
                        this.rpcResponse,
                        this.rpcResponse == null || !this.rpcResponse.IsOk,
                        this.requestStart,
                        this.requestType,
                        this.dateTimeService
                    )
                },
            };

            Result<object> result;

            try
            {
                result = await this.ProcessRequest();
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, "RPC_SERVER_ERROR");
                result = Result.Error<object>($"General RPC error: {errorID}");
            }
            finally
            {
                this.log.Http(() => new HttpLogData(
                    this.httpContext,
                    null,
                    null,
                    null,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart,
                    this.requestType,
                    this.dateTimeService
                ));

                this.log.HttpDetailed(() => new HttpLogData(
                    this.httpContext,
                    // ReSharper disable once AccessToDisposedClosure
                    this.requestBody,
                    this.rpcRequest,
                    this.rpcResponse,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart,
                    this.requestType,
                    this.dateTimeService
                ));

                this.requestBody?.Dispose();
            }

            await this.WriteResult(result);
        }

        private async Task<Result<object>> ProcessRequest()
        {
            // Read request body.
            try
            {
                this.requestBody = await this.httpContext.Request.ReadToEnd();

                if (this.requestBody.Memory.Length == 0)
                {
                    throw new ApplicationException("The request has an empty body.");
                }
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return Result.Error<object>($"Failed to read RPC request body: {errorID}");
            }

            // Find request metadata.
            if (string.IsNullOrWhiteSpace(this.requestType))
            {
                return Result.Error<object>("Request type is null or an empty string.");
            }

            var metadata = this.rpcEngine.GetMetadataByRequestName(this.requestType);

            if (metadata == null)
            {
                return Result.Error<object>($"No RPC handler for request: {this.requestType}.");
            }

            // Parse the RPC message.
            try
            {
                this.rpcRequest = this.DeserializeRequestMessage(metadata);
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return Result.Error<object>($"Failed to parse RPC body: {errorID}");
            }

            // Execute.
            try
            {
                this.rpcResponse = await this.rpcEngine.Execute(this.rpcRequest, this.instanceProvider);
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, new Dictionary<string, object>
                {
                    {"rpcRequest", this.rpcRequest},
                });

                return Result.Error<object>($"RPC execution error ({this.rpcRequest.Type}): {errorID}");
            }

            return this.rpcResponse;
        }

        /// <summary>
        /// Reads RpcRequestMessage from the HTTP request body.
        /// </summary>
        private RpcRequestMessage DeserializeRequestMessage(RpcRequestMetadata metadata)
        {
            try
            {
                var payload = JsonSerializer.Deserialize(this.requestBody.Memory.Span, metadata.RequestType, SerializationOptions);

                var rpcRequestMessage = new RpcRequestMessage
                {
                    Type = this.requestType,
                    Payload = payload,
                };

                return rpcRequestMessage;
            }
            catch (Exception err)
            {
                long? bytePositionInLine = null;
                long? lineNumber = null;
                string jsonPath = null;

                if (err is JsonException jsonException)
                {
                    bytePositionInLine = jsonException.BytePositionInLine;
                    lineNumber = jsonException.LineNumber;
                    jsonPath = jsonException.Path;
                }

                throw new DetailedLogException("Failed to parse RPC message.")
                {
                    Details =
                    {
                        {"bytePositionInLine", bytePositionInLine},
                        {"lineNumber", lineNumber},
                        {"jsonPath", jsonPath},
                    },
                };
            }
        }

        /// <summary>
        /// Writes a <see cref="Result" /> to the HTTP response.
        /// </summary>
        private async ValueTask WriteResult<T>(Result<T> result)
        {
            try
            {
                this.httpContext.Response.StatusCode = 200;

                await JsonSerializer.SerializeAsync(this.httpContext.Response.Body, result, SerializationOptions);
            }
            catch (Exception err)
            {
                await this.errorReporter.Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE", new Dictionary<string, object>
                {
                    {"result", result},
                });
            }
        }
    }

    public class RpcAuthorizationMiddleware : RpcMiddleware
    {
        public const string UNAUTHORIZED_ACCESS_MESSAGE = "Unauthorized access.";

        private static readonly RpcAuthAttribute DefaultAuthAttribute = new RpcAuthAttribute
        {
            RequiresAuthentication = true,
        };

        private readonly HttpRequestState httpRequestState;

        public RpcAuthorizationMiddleware(HttpRequestState httpRequestState)
        {
            this.httpRequestState = httpRequestState;
        }

        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            var authAttribute = context.GetSupplementalAttribute<RpcAuthAttribute>() ?? DefaultAuthAttribute;

            var authResult = this.httpRequestState.AuthResult;

            bool isAuthenticated = authResult.IsAuthenticated && authResult.ValidCsrfToken;

            if (authAttribute.RequiresAuthentication && !isAuthenticated)
            {
                context.SetResponse(Result.Error(UNAUTHORIZED_ACCESS_MESSAGE));
                return;
            }

            context.SetHandlerArgument(authResult);

            await next(context, instanceProvider);
        }
    }

    public class RpcInputValidationMiddleware : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            var validationResult = InputValidator.Validate(context.RequestMessage.Payload);

            if (!validationResult.IsOk)
            {
                context.SetResponse(validationResult);
                return;
            }

            await next(context, instanceProvider);
        }
    }

    public class RpcAuthAttribute : RpcSupplementalAttribute
    {
        public bool RequiresAuthentication { get; set; }
    }

    public class HttpLogData
    {
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

        public HttpLogData(HttpContext context,
            IMemoryOwner<byte> requestBody,
            RpcRequestMessage rpcRequest,
            Result<object> rpcResponse,
            bool requestFailed,
            DateTime requestStart,
            string requestType,
            DateTimeService dateTimeService)
        {
            var now = dateTimeService.CurrentTime();

            this.DateTime = now.ToString("O");
            this.RequestID = context.Connection.Id;
            this.LocalIp = context.Connection.LocalIpAddress + ":" + context.Connection.LocalPort;
            this.RemoteIp = context.Connection.RemoteIpAddress + ":" + context.Connection.RemotePort;
            this.Cookies = context.Request.Cookies.ToDictionary(x => x.Key, x => x.Value);
            this.Headers = context.Request.Headers.ToDictionary(x => x.Key, x => x.Value.ToString());
            this.Method = context.Request.Method;
            this.Path = context.Request.Path.ToString();
            this.Query = context.Request.QueryString.ToString();
            this.Protocol = context.Request.Protocol;
            this.Scheme = context.Request.Scheme;
            this.Aborted = context.RequestAborted.IsCancellationRequested;

            if (requestBody != null)
            {
                this.HttpRequestBodyBase64 = Convert.ToBase64String(requestBody.Memory.Span);
            }

            if (rpcRequest != null)
            {
                this.RpcRequest = JsonSerializer.Serialize(rpcRequest);
            }

            if (rpcResponse != null)
            {
                this.RpcRequest = JsonSerializer.Serialize(rpcResponse);
            }

            this.StatusCode = context.Response.StatusCode;
            this.RequestFailed = requestFailed;
            this.RequestDuration = (now - requestStart).TotalMilliseconds;
            this.RequestType = requestType;
        }

        public string RequestType { get; set; }

        public double RequestDuration { get; set; }

        public int StatusCode { get; set; }

        public string RpcRequest { get; set; }

        public string RequestID { get; set; }

        public string LocalIp { get; set; }

        public string RemoteIp { get; set; }

        public Dictionary<string, string> Cookies { get; set; }

        public Dictionary<string, string> Headers { get; set; }

        public string Method { get; set; }

        public string Path { get; set; }

        public string Query { get; set; }

        public string Protocol { get; set; }

        public string Scheme { get; set; }

        public bool Aborted { get; set; }

        public string HttpRequestBodyBase64 { get; set; }

        public string DateTime { get; set; }

        public bool RequestFailed { get; }

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
}
