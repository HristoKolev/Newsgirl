using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Newsgirl.ApiInvoke
{
    public class ApiClient
    {
        public ApiClient(AppConfig config)
        {
            this.Config = config;
        }

        private AppConfig Config { get; }

        public async Task<ApiResult> Send(ApiRequest dto)
        {
            var request = WebRequest.CreateHttp(new Uri(this.Config.ApiUrl));
            request.Method = "POST";
            request.Timeout = Timeout.Infinite;

            request.ContentType = "application/json";
            request.Accept = "application/json";

            using (var requestStream = await request.GetRequestStreamAsync())
            using (var writer = new StreamWriter(requestStream, Encoding.UTF8))
            {
                var requestJson = JsonConvert.SerializeObject(dto);

                await writer.WriteAsync(requestJson);
            }

            using (var response = await request.GetResponseAsync())
            using (var responseStream = response.GetResponseStream())
            using (var streamReader = new StreamReader(responseStream, Encoding.UTF8))
            {
                var responseJson = await streamReader.ReadToEndAsync();

                return JsonConvert.DeserializeObject<ApiResult>(responseJson);
            }
        }
    }

    public class ApiResult
    {
        public bool Success { get; set; }

        public string[] ErrorMessages { get; set; }

        public object Payload { get; set; }

        public static ApiResult SuccessfulResult()
        {
            return new ApiResult
            {
                Success = true
            };
        }

        public static ApiResult SuccessfulResult(object payload)
        {
            return new ApiResult
            {
                Success = true,
                Payload = payload
            };
        }

        public static ApiResult FromErrorMessage(string message)
        {
            return new ApiResult
            {
                Success = false,
                ErrorMessages = new[] {message}
            };
        }

        public static ApiResult FromErrorMessages(string[] errorMessages)
        {
            return new ApiResult
            {
                Success = false,
                ErrorMessages = errorMessages
            };
        }
    }

    public class ApiRequest
    {
        public string Payload { get; set; }

        public string Type { get; set; }
    }
}