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
    using Shared.Infrastructure;

    /// <summary>
    ///     Serves Rpc requests over HTTP.
    /// </summary>
    public class RpcRequestHandler
    {
        private readonly RpcEngine rpcEngine;
        private readonly ILog log;
        private readonly InstanceProvider instanceProvider;
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static ConcurrentDictionary<Type, Type> genericRpcModelTable;
        private static Func<object, RpcRequestMessage> copyData;
        private static JsonSerializerOptions serializationOptions;
        
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

        public RpcRequestHandler(RpcEngine rpcEngine, ILog log, InstanceProvider instanceProvider)
        {
            this.rpcEngine = rpcEngine;
            this.log = log;
            this.instanceProvider = instanceProvider;
        }

        public async Task HandleRequest(HttpContext context)
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
                string errorID = await this.log.Error(err, "RPC_SERVER_ERROR_BEFORE_READ_REQUEST");
                await this.WriteError(context.Response, $"General RPC error: {errorID}");
                return;
            }

            // Read request body.
            RentedByteArrayHandle requestBodyBytes;

            try
            {
                requestBodyBytes = await context.Request.ReadToEnd();
            }
            catch (Exception err)
            {
                string errorID = await this.log.Error(err);
                await this.WriteError(context.Response, $"Failed to read RPC request body: {errorID}");
                return;
            }

            // Parse the RPC message.
            RpcResult<RpcRequestMessage> requestMessageResult;

            using (requestBodyBytes)
            {
                try
                {
                    requestMessageResult = this.ParseRequestMessage(requestBodyBytes);
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
                    
                    string errorID = await this.log.Error(err, new Dictionary<string, object>
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestBodyBytes.AsSpan())},
                        {"bytePositionInLine", bytePositionInLine},
                        {"lineNumber", lineNumber},
                        {"jsonPath", jsonPath},
                    });
                    await this.WriteError(context.Response, $"Failed to parse RPC body: {errorID}");
                    return;
                }                
            }

            if (!requestMessageResult.IsOk)
            {
                await this.WriteResult(context.Response, requestMessageResult);
                return;
            }

            RpcRequestMessage rpcRequestMessage = requestMessageResult.Payload;
            
            // Execute.
            RpcResult<object> rpcResult;

            try
            {
                rpcResult = await this.rpcEngine.Execute(rpcRequestMessage, this.instanceProvider);
            }
            catch (Exception err)
            {
                string errorID = await this.log.Error(err, new Dictionary<string, object>
                {
                    {"rpcRequestMessage", rpcRequestMessage}
                });
                
                await this.WriteError(context.Response, $"RPC execution error ({rpcRequestMessage.Type}): {errorID}");
                return;
            }
            
            await this.WriteResult(context.Response, rpcResult);
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
        ///     Writes a <see cref="RpcResult{T}" /> to the HTTP result.
        /// </summary>
        private async ValueTask WriteResult<T>(HttpResponse response, RpcResult<T> result)
        {
            try
            {
                response.StatusCode = 200;
                await JsonSerializer.SerializeAsync(response.Body, result, serializationOptions);
            }
            catch (Exception err)
            {
                await this.log.Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE", new Dictionary<string, object>
                {
                    {"result", result}
                });
            }
        }

        private ValueTask WriteError(HttpResponse response, string errorMessage)
        {
            return this.WriteResult(response, RpcResult.Error<object>(errorMessage));
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
}
