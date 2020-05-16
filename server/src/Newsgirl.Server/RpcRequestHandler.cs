namespace Newsgirl.Server
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection.Emit;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared;

    /// <summary>
    ///     Serves Rpc requests over HTTP.
    /// </summary>
    public class RpcRequestHandler
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static ConcurrentDictionary<Type, Type> genericRpcModelTable;
        private static Func<object, RpcRequestMessage> copyData;
        private static JsonSerializerOptions serializationOptions;
        
        private readonly RpcEngine rpcEngine;
        private readonly InstanceProvider instanceProvider;
        private readonly AsyncLocals asyncLocals;
        private readonly ErrorReporter errorReporter;
        private readonly ILog log;
        
        private HttpContext httpContext;
        private RentedByteArrayHandle requestBody;
        private RpcRequestMessage rpcRequest;
        private RpcResult<object> rpcResponse;
        private DateTime requestStart;

        private static void InitializeStaticCache()
        {
            genericRpcModelTable = new ConcurrentDictionary<Type, Type>();
            copyData = CreateCopyDataMethod();
            serializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            };
        }

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
        
        public async Task HandleRequest(HttpContext httpContext)
        {
            this.httpContext = httpContext;
            this.requestStart = DateTime.UtcNow;
            
            // Diagnostic data in case of an error.
            this.asyncLocals.CollectHttpData.Value = () => new Dictionary<string, object>
            {
                {"http", new HttpLogData(
                    httpContext,
                    this.requestBody,
                    this.rpcRequest,
                    this.rpcResponse,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart
                )}
            };

            RpcResult<object> result;

            try
            {
                result = await this.Process(httpContext);
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, "RPC_SERVER_ERROR");
                result = RpcResult.Error<object>($"General RPC error: {errorID}");
            }
            finally
            {
                this.log.Http(() => new HttpLogData(
                    httpContext,
                    null,
                    null,
                    null,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart
                ));
                
                this.log.HttpDetailed(() => new HttpLogData(
                    httpContext,
                    this.requestBody,
                    this.rpcRequest,
                    this.rpcResponse,
                    this.rpcResponse == null || !this.rpcResponse.IsOk,
                    this.requestStart
                ));
                
                this.requestBody?.Dispose();
            }

            await this.WriteResult(result);
        }

        private async Task<RpcResult<object>> Process(HttpContext context)
        {
            // Initialize.            
            try
            {
                if (!initialized)
                {
                    lock (SyncRoot)
                    {
                        if (!initialized)
                        {
                            InitializeStaticCache();
                            initialized = true;
                        }
                    }
                }
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, "RPC_SERVER_ERROR_BEFORE_READ_REQUEST");
                return RpcResult.Error<object>($"General RPC error: {errorID}");
            }

            // Read request body.
            try
            {
                this.requestBody = await context.Request.ReadToEnd();
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err);
                return RpcResult.Error<object>($"Failed to read RPC request body: {errorID}");
            }
            
            // Parse the RPC message.
            RpcResult<RpcRequestMessage> requestMessageResult;
        
            try
            {
                requestMessageResult = this.ParseRequestMessage(this.requestBody);
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
            
                string errorID = await this.errorReporter.Error(err, new Dictionary<string, object>
                {
                    {"bytePositionInLine", bytePositionInLine},
                    {"lineNumber", lineNumber},
                    {"jsonPath", jsonPath},
                });

                return RpcResult.Error<object>($"Failed to parse RPC body: {errorID}");
            }

            if (!requestMessageResult.IsOk)
            {
                return RpcResult.Error<object>(requestMessageResult.ErrorMessages);
            }

            this.rpcRequest = requestMessageResult.Payload;
        
            // Execute.
            try
            {
                this.rpcResponse = await this.rpcEngine.Execute(this.rpcRequest, this.instanceProvider);
            }
            catch (Exception err)
            {
                string errorID = await this.errorReporter.Error(err, new Dictionary<string, object>
                {
                    {"rpcRequest", this.rpcRequest}
                });

                return RpcResult.Error<object>($"RPC execution error ({this.rpcRequest.Type}): {errorID}");
            }

            return this.rpcResponse;
        }

        /// <summary>
        ///     Parses an <see cref="RpcRequestMessage" /> from a <see cref="RentedByteArrayHandle"/>.
        /// </summary>
        private RpcResult<RpcRequestMessage> ParseRequestMessage(RentedByteArrayHandle bufferHandle)
        {
            var typeModel = JsonSerializer.Deserialize<RpcTypeDto>(bufferHandle.AsSpan(), serializationOptions);

            string rpcRequestType = typeModel.Type;

            if (string.IsNullOrWhiteSpace(rpcRequestType))
            {
                return RpcResult.Error<RpcRequestMessage>("Request type is null or an empty string.");
            }

            var metadata = this.rpcEngine.GetMetadataByRequestName(rpcRequestType);

            if (metadata == null)
            {
                return RpcResult.Error<RpcRequestMessage>($"No RPC handler for request: {rpcRequestType}.");
            }

            var deserializeType = genericRpcModelTable.GetOrAdd(
                metadata.RequestType, x => typeof(RpcRequestMessageDto<>).MakeGenericType(x));

            var payloadAndHeaders =
                JsonSerializer.Deserialize(bufferHandle.AsSpan(), deserializeType, serializationOptions);

            var rpcRequestMessage = copyData(payloadAndHeaders);

            return RpcResult.Ok(rpcRequestMessage);
        }

        /// <summary>
        ///     Writes a <see cref="RpcResult" /> to the HTTP result.
        /// </summary>
        private async ValueTask WriteResult<T>(RpcResult<T> result)
        {
            try
            {
                this.httpContext.Response.StatusCode = 200;
                
                await JsonSerializer.SerializeAsync(this.httpContext.Response.Body, result, serializationOptions);
            }
            catch (Exception err)
            {
                await this.errorReporter.Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE", new Dictionary<string, object>
                {
                    {"result", result}
                });
            }
        }

        /// <summary>
        ///     Creates a function that copies properties from an <see cref="RpcRequestMessageDto{T}" />
        ///     instance to an <see cref="RpcRequestMessage" /> instance.
        /// </summary>
        private static Func<object, RpcRequestMessage> CreateCopyDataMethod()
        {
            var copyDataMethod = new DynamicMethod("copyData", typeof(RpcRequestMessage), new[] {typeof(object)});

            var il = copyDataMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(RpcRequestMessage).GetConstructors().First());

            foreach (var property in typeof(RpcRequestMessage).GetProperties())
            {
                il.Emit(OpCodes.Dup);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Call, typeof(RpcRequestMessageDto<>).MakeGenericType(typeof(object)).GetProperty(property.Name)?.GetMethod!);
                il.Emit(OpCodes.Call, property.SetMethod!);    
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
            
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Type { get; set; }
        }

        private struct RpcTypeDto
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Type { get; set; }
        }
    }
    
    public class HttpLogData
    {
        public HttpLogData(HttpContext context,
            RentedByteArrayHandle requestBody,
            RpcRequestMessage rpcRequest,
            RpcResult<object> rpcResponse,
            bool requestFailed,
            DateTime requestStart)
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
                this.HttpRequestBodyBase64 = Convert.ToBase64String(requestBody.AsSpan());
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
        }

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
    }
    
    public static class HttpLoggingExtensions
    {
        public const string HttpKey = "HTTP_REQUESTS";
        public const string HttpDetailedKey = "HTTP_REQUESTS_DETAILED";

        public static void Http(this ILog log, Func<HttpLogData> func) => log.Log(HttpKey, func);
        
        public static void HttpDetailed(this ILog log, Func<HttpLogData> func) => log.Log(HttpDetailedKey, func);
    }
}
