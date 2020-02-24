using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.WebServices.Infrastructure.Api
{
    using System;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;
    using Newtonsoft.Json.Serialization;

    /// <summary>
    /// A helper created to handle Serializing/Deserializing of objects related to the Api Handler protocol.
    /// </summary>
    public static class ApiResultJsonHelper
    {
        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            ContractResolver = new CamelCasePropertyNamesContractResolver()
        };
        
        /// <summary>
        /// It serializes a ApiResult using special settings.
        /// </summary>
        public static string Serialize(ApiResult result)
        {
            return JsonConvert.SerializeObject(result, SerializerSettings);
        }
        
        /// <summary>
        /// Parses a json string to get the `ApiRequest`.
        /// If there is something missing or otherwise wrong it reruns a failure.
        /// </summary>
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