namespace Newsgirl.WebServices
{
    using System;
    using System.Threading.Tasks;

    using Infrastructure;

    public static class Program
    {
        public static async Task<int> Main(string[] args)
        {
            // Settings.
            await Global.ReadSettings();
            
            // Logging.
            MainLogger.Initialize(new LoggerConfigModel
            {
                SentryDsn = Global.AppConfig.SentryDsn,
                LogRootDirectory = Global.DataDirectory
            });

            // Gather handler related metadata.
            Global.LoadHandlers();

            // Read settings from the database.
            using (var container = Global.CreateIoC())
            {
                var settingsService = container.GetInstance<SystemSettingsService>();
                Global.Settings = await settingsService.ReadSettings<SystemSettings>();
            }
            
            // Parse the commandline arguments and decide
            // what kind of process this is going to be.
            var (cliOption, restArgs) = CliParser.Parse(args);

            switch (cliOption)
            {
                case CliOption.WebServer:
                {
                    // Run the web server.
                    return await WebServer.Run(restArgs);
                }
                case CliOption.ApiCall:
                {
                    // Run an api call.
                    return await ApiCall.Run(restArgs);
                }
                case CliOption.GenerateClientRpcCode:
                {
                    // Generate client rpc code.
                    return await RpcCodeGenerator.Generate(restArgs);
                }
                default:
                {
                    throw new ArgumentOutOfRangeException(nameof(cliOption));
                }
            }
        }
    }
}