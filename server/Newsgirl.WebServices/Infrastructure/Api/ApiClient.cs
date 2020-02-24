namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    interface IApiClient
    {
        Task<ApiResult> Call(ApiRequest req);
    }
    
    /// <summary>
    /// ApiClient that uses HTTP to call a remote server that supports the API handler protocol.
    /// </summary>
    // ReSharper disable once UnusedMember.Global
    public class RemoteApiClient : IApiClient
    {
        private string ApiUrl { get; }

        public RemoteApiClient(string apiUrl)
        {
            this.ApiUrl = apiUrl;
        }
        
        public async Task<ApiResult> Call(ApiRequest req)
        {
            var request = WebRequest.CreateHttp(new Uri(this.ApiUrl));
            request.Method = "POST";
            request.Timeout = Timeout.Infinite;

            request.ContentType = "application/json";
            request.Accept = "application/json";

            using (var requestStream = await request.GetRequestStreamAsync())
            using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
            {
                string requestJson = JsonConvert.SerializeObject(req);

                await writer.WriteAsync(requestJson);
            }

            using (var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                string responseJson = await streamReader.ReadToEndAsync();

                return JsonConvert.DeserializeObject<ApiResult>(responseJson);
            }
        }
    }

    /// <summary>
    /// A direct way of using the api protocol internally. 
    /// </summary>
    public class DirectApiClient : IApiClient
    {
        private TypeResolver Resolver { get; }

        public DirectApiClient(TypeResolver resolver)
        {
            this.Resolver = resolver;
        }
            
        public async Task<ApiResult> Call(ApiRequest req)
        {
            return await ApiHandlerProtocol.ProcessRequest(req, Global.Handlers, this.Resolver);
        }
    }
}