namespace Newsgirl.WebServices
{
    using System;
    using System.Threading.Tasks;

    using Autofac;

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

                using (var container = Global.CreateIoC())
                {
                    if (!commandModel.SkipSettingsLoading)
                    {
                        // Read settings from the database.

                        var settingsService = container.Resolve<SystemSettingsService>();
                        Global.Settings = await settingsService.ReadSettings<SystemSettings>();
                    }

                    var command = (ICliCommand) container.Resolve(commandModel.CommandType);

                    return await command.Run(restArgs);
                }
            }
            catch (Exception exception)
            {
                await MainLogger.Instance.LogError(exception);

                return 1;
            }
        }
    }
}