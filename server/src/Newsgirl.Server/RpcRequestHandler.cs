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

                var rpcEngine = instanceProvider.Get<RpcEngine>();

                var requestMessageResult = await ReadRequestMessage(context.Request, rpcEngine);

                if (!requestMessageResult.IsOk)
                {
                    await WriteResult(context.Response, requestMessageResult);
                    return;
                }

                var rpcResult = await rpcEngine.Execute(requestMessageResult.Payload, instanceProvider);

                await WriteResult(context.Response, rpcResult);
            }
            catch (Exception err) when (!(err is DetailedLogException))
            {
                var log = instanceProvider.Get<ILog>();
                await log.Error(err);

                await WriteResult(context.Response, RpcResult.Error<object>("An error occured while handling the RPC request."));
            }
        }

        /// <summary>
        ///     Reads an <see cref="RpcRequestMessage" /> from the HTTP request.
        /// </summary>
        private static async ValueTask<RpcResult<RpcRequestMessage>> ReadRequestMessage(
            HttpRequest request,
            RpcEngine rpcEngine)
        {
            using var bufferHandle = await request.ReadToEnd();

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
        private static Task WriteResult<T>(HttpResponse response, RpcResult<T> result)
        {
            response.StatusCode = 200;
            return JsonSerializer.SerializeAsync(response.Body, result, serializationOptions);
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
        ///     Creates a function that copies properties 'Payload' and 'Headers' from an <see cref="RpcPayloadAndHeadersDto{T}" />
        ///     instance to an <see cref="RpcRequestMessage" /> instance.
        /// </summary>
        private static Func<object, RpcRequestMessage> CreateCopyDataMethod()
        {
            var copyDataMethod = new DynamicMethod("copyData", typeof(RpcRequestMessage), new[] {typeof(object)});

            var il = copyDataMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(RpcRequestMessage).GetConstructors().First());
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call,
                typeof(RpcPayloadAndHeadersDto<>).MakeGenericType(typeof(object)).GetProperty("Payload").GetMethod);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty("Payload").SetMethod);
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call,
                typeof(RpcPayloadAndHeadersDto<>).MakeGenericType(typeof(object)).GetProperty("Headers").GetMethod);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty("Headers").SetMethod);
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
