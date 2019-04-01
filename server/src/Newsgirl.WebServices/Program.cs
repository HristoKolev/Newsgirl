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

            try
            {
                // Gather handler related metadata.
                Global.LoadHandlers();
          
                // Scan for cli tasks.
                CliParser.Scan();
            
                // Parse the commandline arguments and decide
                // what kind of process this is going to be.
                var (commandModel, restArgs) = CliParser.Parse(args);

                if (!commandModel.SkipSettingsLoading)
                {
                    // Read settings from the database.
                    using (var container = Global.CreateIoC())
                    {
                        var settingsService = container.GetInstance<SystemSettingsService>();
                        Global.Settings = await settingsService.ReadSettings<SystemSettings>();
                    }    
                }
                
                var command = (ICliCommand) Activator.CreateInstance(commandModel.CommandType);
            
                return await command.Run(restArgs);
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                return 1;
            }
        }
    }
}