namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using log4net;
    using log4net.Config;

    using SharpRaven;
    using SharpRaven.Data;

    public class MainLogger
    {
        public static readonly MainLogger Instance = new MainLogger();

        private const string LoggerFilePath = "log4net-config.xml";

        private static readonly object SyncLock = new object();

        private static bool _isInitialized;

        private static ILog _log4NetLogger;

        private static RavenClient _ravenClient;

        public static void Initialize(LoggerConfigModel config)
        {
            if (_isInitialized)
            {
                return;
            }

            lock (SyncLock)
            {
                if (_isInitialized)
                {
                    return;
                }

                // Configure log4net.
                var logRepository = LogManager.GetRepository(config.Assembly);

                var configFile = new FileInfo(Path.Combine(config.LogRootDirectory, LoggerFilePath));

                XmlConfigurator.ConfigureAndWatch(logRepository, configFile);

                _log4NetLogger = LogManager.GetLogger(config.Assembly, "Global logger");

                // Configure Sentry.
                _ravenClient = new RavenClient(config.SentryDsn);

                _isInitialized = true;
            }
        }

        public void LogDebug(string message)
        {
            _log4NetLogger.Debug(message);
        }

        public void LogError(string message)
        {
            _log4NetLogger.Error(message);
        }

        public Task LogError(Exception exception)
        {
            var list = GetExceptionChain(exception);

            this.LogError($"Exception was handled. (ExceptionMessage: {exception.Message}, ExceptionType: {string.Join(", ", list.Select(x => x.GetType().Name))}) View the Sentry entry for more details.");

            var detailed = exception as DetailedLogException;

            var extra = new Dictionary<string, object>();

            while (detailed != null)
            {
                foreach (var pair in detailed.Context)
                {
                    extra[pair.Key] = pair.Value;
                }

                detailed = detailed.InnerException as DetailedLogException;
            }

            return _ravenClient.CaptureAsync(new SentryEvent(exception)
            {
                Level = ErrorLevel.Error,
                Extra = extra
            });
        }

        private static List<Exception> GetExceptionChain(Exception exception)
        {
            var list = new List<Exception>();

            while (exception != null)
            {
                list.Add(exception);
                exception = exception.InnerException;
            }

            return list;
        }
    }

    public class LoggerConfigModel
    {
        public Assembly Assembly { get; set; }

        public string LogRootDirectory { get; set; }

        public string SentryDsn { get; set; }
    }

    public class DetailedLogException : Exception
    {
        public DetailedLogException()
        {
            this.Context = new Dictionary<string, object>();
        }

        public DetailedLogException(string message)
            : base(message)
        {
            this.Context = new Dictionary<string, object>();
        }

        public DetailedLogException(string message, Exception inner)
            : base(message, inner)
        {
            this.Context = new Dictionary<string, object>();
        }

        public Dictionary<string, object> Context { get; set; }
    }
}