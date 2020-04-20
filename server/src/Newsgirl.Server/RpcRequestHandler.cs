namespace Newsgirl.Server
{
    using System;
    using System.Buffers;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Microsoft.AspNetCore.Http;
    using Shared;
    using Shared.Infrastructure;

    public static class RpcRequestHandler
    {
        public static async Task HandleRequest(InstanceProvider instanceProvider, HttpContext context)
        {
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

                var rpcEngine = instanceProvider.Get<RpcEngine>();
                //
                // Type requestType = rpcEngine.Metadata;
                //
                // var payload = JsonSerializer.Deserialize<MyModel>(buffer.AsSpan(0, offset));

            }
            finally
            {
                bufferPool.Return(buffer);    
            }
        }
    }
}
