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

    public static class RpcRequestHandler
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static ConcurrentDictionary<Type, Type> genericRpcModelTable;
        private static Func<object, RpcRequestMessage> copyData;
        private static JsonSerializerOptions serializationOptions;

        private static async ValueTask<RpcResult<RpcRequestMessage>> ReadRequestMessage(HttpRequest request, InstanceProvider instanceProvider)
        {
            using var bufferHandle = await request.ReadToEnd();

            // TODO: Error handling for bad json
            var typeModel = JsonSerializer.Deserialize<RpcTypeDto>(bufferHandle.AsSpan(), serializationOptions);

            string rpcRequestType = typeModel.Type;

            if (string.IsNullOrWhiteSpace(rpcRequestType))
            {
                return RpcResult.Error<RpcRequestMessage>("Request type is null or an empty string.");
            }

            var rpcEngine = instanceProvider.Get<RpcEngine>();

            var metadata = rpcEngine.GetMetadataByRequestName(rpcRequestType);

            if (metadata == null)
            {
                return RpcResult.Error<RpcRequestMessage>($"No RPC handler for request `{rpcRequestType}`.");
            }

            var deserializeType = genericRpcModelTable.GetOrAdd(
                metadata.RequestType,x => typeof(RpcPayloadAndHeadersDto<>).MakeGenericType(x));

            // TODO: Error handling for bad json
            var payloadAndHeaders = JsonSerializer.Deserialize(bufferHandle.AsSpan(), deserializeType, serializationOptions);

            var rpcRequestMessage = copyData(payloadAndHeaders);

            return RpcResult.Ok(rpcRequestMessage);
        }
        
        public static async ValueTask HandleRequest(InstanceProvider instanceProvider, HttpContext context)
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

            var requestMessageResult = await ReadRequestMessage(context.Request, instanceProvider);

            if (!requestMessageResult.IsOk)
            {
                throw new NotImplementedException();
            }
            
            var rpcEngine = instanceProvider.Get<RpcEngine>();

            var rpcResult = await rpcEngine.Execute(requestMessageResult.Payload, instanceProvider);
            //
            // await using var w = new Utf8JsonWriter(context.Response.BodyWriter);
            //
            // await JsonSerializer.SerializeAsync<>(w, rpcResult, serializationOptions);
        }

        private static void InitializeStaticCache()
        {
            genericRpcModelTable = new ConcurrentDictionary<Type, Type>();
            
            copyData = CreateCopyDataMethod();

            serializationOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };
        }

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
            public T Payload { get; set; }

            public Dictionary<string, string> Headers { get; set; }
        }

        private struct RpcTypeDto
        {
            public string Type { get; set; }
        }
    }
}
