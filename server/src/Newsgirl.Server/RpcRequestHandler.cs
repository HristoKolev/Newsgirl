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
    using Shared.Infrastructure;

    public static class RpcRequestHandler
    {
        private static readonly object SyncRoot = new object();
        private static bool initialized;
        private static ConcurrentDictionary<Type, Type> genericRpcModelTable;
        private static Func<object, RpcRequestMessage> copyData;

        public static async Task HandleRequest(InstanceProvider instanceProvider, HttpContext context)
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

            var request = context.Request;

            // ReSharper disable once PossibleInvalidOperationException
            int contentLength = (int) request.ContentLength.Value;

            // default pool
            var bufferPool = ArrayPool<byte>.Shared;
            var requestStream = request.Body;

            var buffer = bufferPool.Rent(contentLength);

            try
            {
                int read;
                int offset = 0;

                while ((read = await requestStream.ReadAsync(buffer, offset, contentLength - offset)) > 0)
                {
                    offset += read;
                }
            }
            catch (Exception err)
            {
                bufferPool.Return(buffer);

                throw new DetailedLogException("An error occurred while reading the HTTP request body.", err)
                {
                    Fingerprint = "HTTP_FAILED_TO_READ_REQUEST_BODY",
                    Details =
                    {
                        {"contentLength", contentLength}
                    }
                };
            }

            try
            {
                string rpcRequestType;

                using (var jsonDocument = JsonDocument.Parse(buffer.AsMemory(0, contentLength)))
                {
                    rpcRequestType = jsonDocument.RootElement.GetProperty("type").GetString();
                }

                // TODO: Return error if rpcRequestType is null or empty.

                var rpcEngine = instanceProvider.Get<RpcEngine>();

                var metadata = rpcEngine.GetMetadataByRequestName(rpcRequestType);

                if (metadata == null)
                {
                    // TODO: Return error if metadata is null.
                }

                var requestType = metadata.RequestType;

                var deserializeType = genericRpcModelTable.GetOrAdd(requestType,
                    x => typeof(RpcRequestMessageModel<>).MakeGenericType(x));

                var dto = JsonSerializer.Deserialize(buffer.AsSpan(0, contentLength), deserializeType);

                var rpcRequestMessage = copyData(dto);
            }
            finally
            {
                bufferPool.Return(buffer);
            }
        }

        private static void InitializeStaticCache()
        {
            genericRpcModelTable = new ConcurrentDictionary<Type, Type>();
            
            var copyDataMethod = new DynamicMethod("copyData", typeof(RpcRequestMessage), new []{typeof(object)});

            var il = copyDataMethod.GetILGenerator();
            il.Emit(OpCodes.Newobj, typeof(RpcRequestMessage).GetConstructors().First());
            il.Emit(OpCodes.Dup);
            il.Emit(OpCodes.Dup);

            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessageModel<>).MakeGenericType(typeof(object)).GetProperty("Payload").GetMethod);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty("Payload").SetMethod);
            il.Emit(OpCodes.Ldarg_0);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessageModel<>).MakeGenericType(typeof(object)).GetProperty("Headers").GetMethod);
            // ReSharper disable once PossibleNullReferenceException
            // ReSharper disable once AssignNullToNotNullAttribute
            il.Emit(OpCodes.Call, typeof(RpcRequestMessage).GetProperty("Headers").SetMethod);
            il.Emit(OpCodes.Ret);
            
            copyData = (Func<object, RpcRequestMessage>)copyDataMethod.CreateDelegate(typeof(Func<object, RpcRequestMessage>));
        }
    }

    public class RpcRequestMessageModel<T>
    {
        public T Payload { get; set; }

        public Dictionary<string, string> Headers { get; set; }
    }
}
