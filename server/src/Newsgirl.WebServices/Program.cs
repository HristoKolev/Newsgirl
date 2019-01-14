namespace Newsgirl.WebServices
{
    using System;
    using System.IO;
    using System.Net;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Infrastructure;

    using Microsoft.AspNetCore.Hosting;
    using Microsoft.AspNetCore.Server.Kestrel.Core;

    using Newtonsoft.Json;

    using Npgsql;

    using StructureMap;

    public class Program
    {
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