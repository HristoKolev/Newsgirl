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
    public static class RpcRequestHandler
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static ConcurrentDictionary<Type, Type> genericRpcModelTable;
        private static Func<object, RpcRequestMessage> copyData;
        private static JsonSerializerOptions serializationOptions;

        public static async Task HandleRequest(HttpContext context, InstanceProvider instanceProvider)
        {
            // Initialize.
            RpcEngine rpcEngine;
            
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

                rpcEngine = instanceProvider.Get<RpcEngine>();
            }
            catch (Exception err)
            {
                string errorID = await instanceProvider.Get<ILog>().Error(err, "RPC_SERVER_ERROR_BEFORE_READ_REQUEST");
                await WriteError(context.Response, instanceProvider, $"General RPC error: {errorID}");
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
                string errorID = await instanceProvider.Get<ILog>().Error(err);
                await WriteError(context.Response, instanceProvider, $"General RPC error: {errorID}");
                return;
            }

            // Parse the RPC message.
            RpcResult<RpcRequestMessage> requestMessageResult;

            using (requestBodyBytes)
            {
                try
                {
                    requestMessageResult = ParseRequestMessage(requestBodyBytes, rpcEngine);
                }
                catch (Exception err)
                {
                    string errorID = await instanceProvider.Get<ILog>().Error(err, new Dictionary<string, object>()
                    {
                        {"requestBodyBytes", Convert.ToBase64String(requestBodyBytes.AsSpan())}
                    });
                    await WriteError(context.Response, instanceProvider, $"General RPC error: {errorID}");
                    return;
                }                
            }

            if (!requestMessageResult.IsOk)
            {
                await WriteResult(context.Response, instanceProvider, requestMessageResult);
                return;
            }

            RpcRequestMessage rpcRequestMessage = requestMessageResult.Payload;
            
            // Execute.
            RpcResult<object> rpcResult;

            try
            {
                rpcResult = await rpcEngine.Execute(rpcRequestMessage, instanceProvider);
            }
            catch (Exception err)
            {
                string errorID = await instanceProvider.Get<ILog>().Error(err, new Dictionary<string, object>
                {
                    {"rpcRequestMessage", rpcRequestMessage}
                });
                
                await WriteError(context.Response, instanceProvider, $"RPC error ({rpcRequestMessage.Type}): {errorID}");
                return;
            }
            
            await WriteResult(context.Response, instanceProvider, rpcResult);
        }

        /// <summary>
        ///     Parses an <see cref="RpcRequestMessage" /> from a <see cref="RentedByteArrayHandle"/>.
        /// </summary>
        private static RpcResult<RpcRequestMessage> ParseRequestMessage(
            RentedByteArrayHandle bufferHandle, 
            RpcEngine rpcEngine)
        {
            // TODO: Error handling for bad json
            var typeModel = JsonSerializer.Deserialize<RpcTypeDto>(bufferHandle.AsSpan(), serializationOptions);

            string rpcRequestType = typeModel.Type;

            if (string.IsNullOrWhiteSpace(rpcRequestType))
            {
                return RpcResult.Error<RpcRequestMessage>("Request type is null or an empty string.");
            }

            var metadata = rpcEngine.GetMetadataByRequestName(rpcRequestType);

            if (metadata == null)
            {
                return RpcResult.Error<RpcRequestMessage>($"No RPC handler for request `{rpcRequestType}`.");
            }

            var deserializeType = genericRpcModelTable.GetOrAdd(
                metadata.RequestType, x => typeof(RpcPayloadAndHeadersDto<>).MakeGenericType(x));

            // TODO: Error handling for bad json
            var payloadAndHeaders =
                JsonSerializer.Deserialize(bufferHandle.AsSpan(), deserializeType, serializationOptions);

            var rpcRequestMessage = copyData(payloadAndHeaders);

            return RpcResult.Ok(rpcRequestMessage);
        }

        /// <summary>
        ///     Writes a <see cref="RpcResult{T}" /> to the HTTP result.
        /// </summary>
        private static async ValueTask WriteResult<T>(this HttpResponse response, InstanceProvider instanceProvider, RpcResult<T> result)
        {
            try
            {
                response.StatusCode = 200;
                await JsonSerializer.SerializeAsync(response.Body, result, serializationOptions);
            }
            catch (Exception err)
            {
                await instanceProvider.Get<ILog>().Error(err, "RPC_SERVER_FAILED_TO_WRITE_RESPONSE", new Dictionary<string, object>
                {
                    {"result", result}
                });
            }
        }

        private static ValueTask WriteError(this HttpResponse response, InstanceProvider instanceProvider, string errorMessage)
        {
            return WriteResult(response, instanceProvider, RpcResult.Error<object>(errorMessage));
        }
        
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

        /// <summary>
        ///     Creates a function that copies properties from an <see cref="RpcPayloadAndHeadersDto{T}" />
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
                il.Emit(OpCodes.Call, typeof(RpcPayloadAndHeadersDto<>).MakeGenericType(typeof(object)).GetProperty(property.Name)?.GetMethod!);
                il.Emit(OpCodes.Call, property.SetMethod!);    
            }

            il.Emit(OpCodes.Ret);

            return copyDataMethod.CreateDelegate<Func<object, RpcRequestMessage>>();
        }

        private class RpcPayloadAndHeadersDto<T>
        {
            // ReSharper disable once UnusedMember.Local
            public T Payload { get; set; }

            // ReSharper disable once UnusedMember.Local
            public Dictionary<string, string> Headers { get; set; }
        }

        private struct RpcTypeDto
        {
            // ReSharper disable once UnusedAutoPropertyAccessor.Local
            public string Type { get; set; }
        }
    }
}
