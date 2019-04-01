namespace Newsgirl.WebServices.Infrastructure
{
    using System.IO;
    using System.Reflection;
    using System.Threading.Tasks;

    using Api;

    using Microsoft.Extensions.DependencyInjection;

    using Newtonsoft.Json;

    using StructureMap;

    public static class Global
    {
        const string AppSettingsFileName = "app-settings.json";
        
        /// <summary>
        /// Contains the settings vital for application startup.
        /// </summary>
        public static AppConfig AppConfig { get; private set; }
        
        /// <summary>
        /// Reads the settings from a json file.
        /// </summary>
        public static async Task ReadSettings()
        {
            string json = await File.ReadAllTextAsync(Path.Join(DataDirectory, AppSettingsFileName));

            AppConfig = JsonConvert.DeserializeObject<AppConfig>(json);
        }

        /// <summary>
        /// The data directory.
        /// </summary>
        public static string DataDirectory => Debug ? RootDirectory : "/data";

        /// <summary>
        /// The root directory of the project.
        /// </summary>
        public static string RootDirectory => Directory.GetCurrentDirectory();

        /// <summary>
        /// Stores handler metadata.
        /// </summary>
        public static HandlerCollection Handlers { get; private set; }
        
        /// <summary>
        /// Gathers handler metadata from the specified assemblies.
        /// </summary>
        public static void LoadHandlers()
        {
            Handlers = ApiHandlerProtocol.ScanForHandlers(Assembly.GetExecutingAssembly());
        }
        
        public static bool Debug => debug;
        
        #if DEBUG
        // ReSharper disable once InconsistentNaming
        private const bool debug = true;
        #else
        // ReSharper disable once InconsistentNaming
        private const bool debug = false;
        #endif

        /// <summary>
        /// Stores the settings loaded from the database table `system_settings`.
        /// </summary>
        public static SystemSettings Settings { get; set; }

        /// <summary>
        /// The main logger instance.
        /// </summary>
        public static MainLogger Log => MainLogger.Instance;
    
        /// <summary>
        /// Creates a IoC instance.
        /// `IServiceCollection` can be passed if it's used in ASP.NET Core context.
        /// </summary>
        public static IContainer CreateIoC(IServiceCollection serviceCollection = null)
        {
            return new Container(x =>
            {
                if (serviceCollection != null)
                {
                    x.Populate(serviceCollection);
                }
                
                x.AddRegistry<MainRegistry>();
            });
        }
    }

    /// <summary>
    /// Contains settings vital to the application startup.
    /// </summary>
    public class AppConfig
    {
        /// <summary>
        /// The database connection string.
        /// </summary>
        public string ConnectionString { get; set; }

        /// <summary>
        /// The DSN for the Sentry error logging system.
        /// </summary>
        public string SentryDsn { get; set; }
    }

    /// <summary>
    /// Settings read from the database.
    /// </summary>
    public class SystemSettings
    {
        /// <summary>
        /// The port that the web server is going to listen on.
        /// </summary>
        public int WebServerPort { get; set; }

        /// <summary>
        /// The UserAgent used for http calls to the RSS endpoints.
        /// </summary>
        public string HttpClientUserAgent { get; set; }
    }
}