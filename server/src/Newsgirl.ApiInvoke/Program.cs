using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Newsgirl.ApiInvoke
{
    public class Program
    {
        private static async Task<int> Main(string[] args)
        {
            // Read the app-config.
            if (!File.Exists(Global.AppConfigLocation))
            {
                Console.WriteLine("`app-config.json` is missing...");
                return 1;
            }

            Global.AppConfig = JsonConvert.DeserializeObject<AppConfig>(File.ReadAllText(Global.AppConfigLocation));

            // Logging.
            MainLogger.Initialize(new LoggerConfigModel
            {
                Assembly = Assembly.GetEntryAssembly(),
                LogRootDirectory = Global.ConfigDirectory,
                SentryDsn = Global.AppConfig.SentryDsn
            });

            try
            {
                var (type, payload) = ParseRequest(args);
                var apiClient = new ApiClient(Global.AppConfig);

                var request = new ApiRequest
                {
                    Type = type,
                    Payload = payload
                };

                var response = await apiClient.Send(request);

                Console.WriteLine(JsonConvert.SerializeObject(response, Formatting.Indented));

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
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);
                return 1;
            }

            return 0;
        }

        private static (string, object) ParseRequest(string[] args)
        {
            var type = args[0];

            var arguments = args.Skip(1)
                                .Select(a => a.Split('='))
                                .ToDictionary(pair => pair[0], pair => pair[1]);

            var obj = new JObject();

            foreach (var pair in arguments)
            {
                obj[pair.Key] = pair.Value;
            }
            
            return (type, obj);
        }
    }
}