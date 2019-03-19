namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    using Api;

    using Newtonsoft.Json.Linq;

    /// <summary>
    /// This module parses ApiRequests from the command line
    /// and executes them in a newly created context.  
    /// </summary>
    public static class ApiCall
    {
        /// <summary>
        /// Parses an `ApiRequest` from commandline arguments.
        /// </summary>
        private static ApiRequest ParseRequest(string[] args)
        {
            string type = args[0];

            var arguments = args.Skip(1)
                                .Select(a => a.Split('='))
                                .ToDictionary(pair => pair[0], pair => pair[1]);

            var obj = new JObject();

            foreach (var pair in arguments)
            {
                obj[pair.Key] = pair.Value;
            }

            return new ApiRequest
            {
                Type = type,
                Payload = obj
            };
        }

        public static async Task<int> Run(string[] args)
        {
            try
            {
                using (var container = Global.CreateIoC())
                {
                    var apiClient = container.GetInstance<IApiClient>();
                
                    var request = ParseRequest(args);

                    var response = await apiClient.Call(request);

                    if (!response.Success)
                    {
                        throw new DetailedLogException("A request failed.")
                        {
                            Context =
                            {
                                {"request-json", request},
                                {"response-json", response}
                            }
                        };
                    }
                }
                
                return 0;
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                return 1;
            }
        }
    }
}