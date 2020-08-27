namespace Newsgirl.Server
{
    using System;
    using System.Buffers;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;
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

        private static readonly Func<object, RpcRequestMessage> CopyData = CreateCopyDataMethod();
        private static readonly ConcurrentDictionary<Type, Type> GenericRpcModelTable = new ConcurrentDictionary<Type, Type>();

        private readonly RpcEngine rpcEngine;
        private readonly InstanceProvider instanceProvider;
        private readonly AsyncLocals asyncLocals;
        private readonly ErrorReporter errorReporter;
        private readonly ILog log;

        private HttpContext httpContext;
        private IMemoryOwner<byte> requestBody;
        private RpcRequestMessage rpcRequest;
        private RpcResult<object> rpcResponse;
        private DateTime requestStart;
        private string requestType;

        public RpcRequestHandler(
            RpcEngine rpcEngine,
            InstanceProvider instanceProvider,
            AsyncLocals asyncLocals,
            ErrorReporter errorReporter,
            ILog log)
        {
            this.rpcEngine = rpcEngine;
            this.instanceProvider = instanceProvider;
            this.asyncLocals = asyncLocals;
            this.errorReporter = errorReporter;
            this.log = log;
        }

        public async Task HandleRequest(HttpContext ctx, string rpcRequestType)
        {
            this.httpContext = ctx;
            this.requestStart = DateTime.UtcNow;
            this.requestType = rpcRequestType;

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
                        this.requestType
                    )
                },
            };

            RpcResult<object> result;

            try
            {
                result = await this.ProcessRequest();
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, "RPC_SERVER_ERROR");
                result = RpcResult.Error<object>($"General RPC error: {errorID}");
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
                    this.requestType
                ));

                this.log.HttpDetailed(() => new HttpLogData(
                    this.httpContext,
                    // ReSharper disable once AccessToDisposedClosure
                    this.requestBody,
                    this.rpcRequest,
                    this.rpcResponse,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart,
                    this.requestType
                ));

                this.requestBody?.Dispose();
            }

            await this.WriteResult(result);
        }

        private async Task<RpcResult<object>> ProcessRequest()
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
                return RpcResult.Error<object>($"Failed to read RPC request body: {errorID}");
            }

            // Find request metadata
            if (string.IsNullOrWhiteSpace(this.requestType))
            {
                return RpcResult.Error<object>("Request type is null or an empty string.");
            }

            var metadata = this.rpcEngine.GetMetadataByRequestName(this.requestType);

            if (metadata == null)
            {
                return RpcResult.Error<object>($"No RPC handler for request: {this.requestType}.");
            }

            // Parse the RPC message.
            try
            {
                var rpcRequestMessage = DeserializeRequestMessage(this.requestBody, metadata);
                rpcRequestMessage.Type = this.requestType;

                this.rpcRequest = rpcRequestMessage;
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return RpcResult.Error<object>($"Failed to parse RPC body: {errorID}");
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

                return RpcResult.Error<object>($"RPC execution error ({this.rpcRequest.Type}): {errorID}");
            }

            return this.rpcResponse;
        }

        /// <summary>
        /// Reads RpcRequestMessage from the HTTP request body.
        /// </summary>
        private static RpcRequestMessage DeserializeRequestMessage(IMemoryOwner<byte> requestBody, RpcRequestMetadata metadata)
        {
            try
            {
                var deserializeType = GenericRpcModelTable.GetOrAdd(metadata.RequestType, x => typeof(RpcRequestMessageDto<>).MakeGenericType(x));

                var payloadAndHeaders = JsonSerializer.Deserialize(requestBody.Memory.Span, deserializeType, SerializationOptions);

                var rpcRequestMessage = CopyData(payloadAndHeaders);

                rpcRequestMessage.Headers ??= new Dictionary<string, string>(0);

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

                throw new DetailedLogException("Failed to parse RPC body.")
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
        /// Writes a <see cref="RpcResult" /> to the HTTP response.
        /// </summary>
        private async ValueTask WriteResult<T>(RpcResult<T> result)
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

        /// <summary>
        /// Creates a function that copies properties from an RpcRequestMessageDto{T}
        /// instance to an RpcRequestMessage instance.
        /// </summary>
        private static Func<object, RpcRequestMessage> CreateCopyDataMethod()
        {
            var copyDataMethod = new DynamicMethod("copyData", typeof(RpcRequestMessage), new[] {typeof(object)});

            var il = copyDataMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(RpcRequestMessage).GetConstructors().First());

            foreach (var property in typeof(RpcRequestMessageDto<object>).GetProperties())
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, property.GetMethod!);
                il.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty(property.Name)?.SetMethod!);
            }

            il.Emit(OpCodes.Ret);

            return copyDataMethod.CreateDelegate<Func<object, RpcRequestMessage>>();
        }

        private class RpcRequestMessageDto<T>
        {
            // ReSharper disable once UnusedMember.Local
            public T Payload { get; set; }

            // ReSharper disable once UnusedMember.Local
            public Dictionary<string, string> Headers { get; set; }
        }

        public static string ParseRequestType(HttpContext context)
        {
            var requestPath = context.Request.Path;

            const string RPC_ROUTE_PATH = "/rpc/";

            if (requestPath.HasValue
                && requestPath.Value.StartsWith(RPC_ROUTE_PATH)
                && requestPath.Value.Length > RPC_ROUTE_PATH.Length)
            {
                string requestType = requestPath.Value.Remove(0, RPC_ROUTE_PATH.Length);

                return requestType;
            }

            return null;
        }
    }

    public class HttpLogData
    {
        // ReSharper disable MemberCanBePrivate.Global
        // ReSharper disable AutoPropertyCanBeMadeGetOnly.Global

        public HttpLogData(HttpContext context,
            IMemoryOwner<byte> requestBody,
            RpcRequestMessage rpcRequest,
            RpcResult<object> rpcResponse,
            bool requestFailed,
            DateTime requestStart,
            string requestType)
        {
            var now = System.DateTime.UtcNow;

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
        public const string HttpKey = "HTTP_REQUESTS";
        public const string HttpDetailedKey = "HTTP_REQUESTS_DETAILED";

        public static void Http(this ILog log, Func<HttpLogData> func)
        {
            log.Log(HttpKey, func);
        }

        public static void HttpDetailed(this ILog log, Func<HttpLogData> func)
        {
            log.Log(HttpDetailedKey, func);
        }
    }
}
