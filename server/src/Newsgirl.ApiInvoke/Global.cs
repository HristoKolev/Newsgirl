using System.IO;
using System.Reflection;

namespace Newsgirl.ApiInvoke
{
    public static class Global
    {
        private const string AppConfigFileName = "app-settings.json";

        public static AppConfig AppConfig { get; set; }

        public static string ConfigDirectory => Path.Combine(AssemblyDirectory, "config");

        public static string AppConfigLocation => Path.Combine(ConfigDirectory, AppConfigFileName);

        public static string AssemblyDirectory => Path.GetDirectoryName(Assembly.GetEntryAssembly().Location);
    }

    public class AppConfig
    {
        public string SentryDsn { get; set; }

        public string ApiUrl { get; set; }
    }
}