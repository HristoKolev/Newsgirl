namespace Newsgirl.WebServices.Infrastructure
{
    using System.IO;
    using System.Runtime.InteropServices;

    public static class Global
    {
        public static AppConfig AppConfig { get; set; }

        public static string DataDirectory
        {
            get
            {
                if (Debug || RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                {
                    return RootDirectory;
                }

                return Path.Combine(RootDirectory, "/data");
            }
        }

        public static string RootDirectory => Directory.GetCurrentDirectory();

        public static HandlerCollection Handlers { get; set; }

        #if DEBUG

        // ReSharper disable once InconsistentNaming
        private const bool debug = true;
        #else
        // ReSharper disable once InconsistentNaming
        private const bool debug = false;
        #endif

        public static bool Debug => debug;

        public static SystemSettings Settings { get; set; }

        public static MainLogger Log => MainLogger.Instance;
    }

    public class AppConfig
    {
        public string AspNetLoggingLevel { get; set; }

        public string ConnectionString { get; set; }

        public string SentryDsn { get; set; }

        public int Port { get; set; }
    }

    public class SystemSettings
    {
    }
}