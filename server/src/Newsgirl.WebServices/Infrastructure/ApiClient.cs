namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.IO;
    using System.Net;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;

    using Newtonsoft.Json;

    public class ApiClient
    {
        public async Task<ApiResult> Send(ApiRequest dto)
        {
            var request = WebRequest.CreateHttp(new Uri(Global.AppConfig.ApiUrl));
            request.Method = "POST";
            request.Timeout = Timeout.Infinite;

            request.ContentType = "application/json";
            request.Accept = "application/json";

            using (var requestStream = await request.GetRequestStreamAsync())
            using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
            {
                string requestJson = JsonConvert.SerializeObject(dto);

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
}