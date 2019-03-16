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

    public class DirectApiClient : IApiClient
    {
        private TypeResolver ServiceProvider { get; }

        public DirectApiClient(TypeResolver serviceProvider)
        {
            this.ServiceProvider = serviceProvider;
        }
            
        public async Task<ApiResult> Call(ApiRequest req)
        {
            return await ApiHandlerProtocol.ProcessRequest(
                req,
                Global.Handlers, 
                this.ServiceProvider
            );
        }
    }
}