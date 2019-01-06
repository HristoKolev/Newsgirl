using System;
using System.IO;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Newsgirl.WebServices.Infrastructure;
using Newtonsoft.Json;
using Npgsql;
using StructureMap;

namespace Newsgirl.WebServices
{
    public class Program
    {
        public static async Task<int> Main(string[] args)
        {
            Global.AppConfig =
                JsonConvert.DeserializeObject<AppConfig>(
                    File.ReadAllText(Path.Join(Global.DataDirectory, "appsettings.json")));
            Global.AppConfig.ConnectionString = CreateConnectionString(Global.AppConfig.ConnectionString);

            MainLogger.Initialize(Assembly.GetExecutingAssembly());

            Global.Handlers = ApiHandlerProtocol.ScanForHandlers(Assembly.GetExecutingAssembly());

            using (var container = new Container(x => x.AddRegistry<MainRegistry>()))
            {
                var settingsService = container.GetInstance<SystemSettingsService>();
                Global.Settings = await settingsService.ReadSettings<SystemSettings>();
            }

            return await RunWebServer();
        }

        private static string CreateConnectionString(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false
            };

            return builder.ToString();
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
    }
}