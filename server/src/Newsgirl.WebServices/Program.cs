namespace Newsgirl.WebServices
{
    using System;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Infrastructure;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;

    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    using Npgsql;

    using StructureMap;

    public class Program
    {
        const string ApiCallParameter = "api-call";
        
        public static async Task<int> Main(string[] args)
        {
            Global.AppConfig =
                JsonConvert.DeserializeObject<AppConfig>(
                    File.ReadAllText(Path.Join(Global.DataDirectory, "appsettings.json")));

            Global.AppConfig.ConnectionString = CreateConnectionString(Global.AppConfig.ConnectionString);

            ObjectPool<X509Certificate2>.SetFactory(async () =>
            {
                var certBytes = await File.ReadAllBytesAsync(Path.Combine(Global.DataDirectory, "certificate.pfx"));
                
                return new X509Certificate2(certBytes);
            });
            
            MainLogger.Initialize(new LoggerConfigModel
            {
                Assembly = Assembly.GetExecutingAssembly(),
                SentryDsn = Global.AppConfig.SentryDsn,
                LogRootDirectory = Global.DataDirectory
            });

            Global.Handlers = ApiHandlerProtocol.ScanForHandlers(Assembly.GetExecutingAssembly());

            using (var container = new Container(x => x.AddRegistry<MainRegistry>()))
            {
                var settingsService = container.GetInstance<SystemSettingsService>();
                Global.Settings = await settingsService.ReadSettings<SystemSettings>();
            }

            string firstParameter = args.FirstOrDefault();

            if (firstParameter == ApiCallParameter)
            {
                var restArgs = args.Skip(1).ToArray();
                
                return await ApiCall(restArgs);
            }

            return await RunWebServer();
        }

        private static async Task<int> ApiCall(string[] args)
        {
            try
            {
                var apiClient = new ApiClient();
                
                (string type, var payload) = ParseRequest(args);

                var request = new ApiRequest
                {
                    Type = type,
                    Payload = payload
                };

                var response = await apiClient.Send(request);

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

                return 0;
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                return 1;
            }
        }

        private static async Task<int> RunWebServer()
        {
            try
            {
                void ConfigureKestrel(KestrelServerOptions opt)
                {
                    opt.AddServerHeader = false;
                    opt.Listen(IPAddress.Any, Global.AppConfig.Port);
                }

                var builder = new WebHostBuilder();

                builder.UseKestrel(ConfigureKestrel)
                       .UseContentRoot(Global.RootDirectory)
                       .UseStartup<Startup>()
                       .Build()
                       .Run();
            }
            catch (Exception ex)
            {
                await Global.Log.LogError(ex);

                return 1;
            }

            return 0;
        }
        
        private static (string, object) ParseRequest(string[] args)
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

            return (type, obj);
        }
        
        public static string CreateConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false
            };

            return builder.ToString();
        }
    }
}