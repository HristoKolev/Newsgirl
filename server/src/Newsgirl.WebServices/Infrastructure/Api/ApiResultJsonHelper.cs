namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    public static class ApiResultJsonHelper
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        
        public static string Serialize(ApiResult result)
        {
            return JsonConvert.SerializeObject(result, SerializerSettings);
        }
        
        public static Result<ApiRequest> TryParse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return Result.FromErrorMessage<ApiRequest>("The request body is empty.");
            }

            var jsonRequest = JObject.Parse(json);
            
            string requestType = jsonRequest.GetValue("type", StringComparison.InvariantCultureIgnoreCase).ToString();
            
            if (string.IsNullOrWhiteSpace(requestType))
            {
                return Result.FromErrorMessage<ApiRequest>("The request type is empty.");
            }

            var handler = Global.Handlers.GetHandler(requestType);

            if (handler == null)
            {
                return Result.FromErrorMessage<ApiRequest>($"No handler found for request type `{requestType}`.");
            }

            string requestPayloadJson = jsonRequest.GetValue("payload", StringComparison.InvariantCultureIgnoreCase).ToString();

            object requestPayload = JsonConvert.DeserializeObject(requestPayloadJson, handler.RequestType, SerializerSettings);
            
            return Result.Success(new ApiRequest
            {
                Type = requestType,
                Payload = requestPayload
            });
        } 
    }
}