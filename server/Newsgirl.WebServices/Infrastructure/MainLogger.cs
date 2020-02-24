namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;

    using log4net;
    using log4net.Config;

    using SharpRaven;
    using SharpRaven.Data;

    /// <summary>
    /// Logging module that wraps around Log4net and RavenClient (Sentry).
    /// </summary>
    public class MainLogger
    {
        /// <summary>
        /// A singleton instance for use in places where one has no access to DI.
        /// </summary>
        public static readonly MainLogger Instance = new MainLogger();

        /// <summary>
        /// The filename of the Log4Net config file.
        /// </summary>
        private const string LoggerFilePath = "log4net-config.xml";

        /// <summary>
        /// Sync object used exclusively for logging.
        /// </summary>
        private static readonly object SyncLock = new object();

        /// <summary>
        /// Used to ensure that the static initialization gets called only once.
        /// </summary>
        private static bool _isInitialized;

        /// <summary>
        /// The Log4Net logger object.
        /// </summary>
        private static ILog _log4NetLogger;

        /// <summary>
        /// The Sentry client.
        /// </summary>
        private static RavenClient _ravenClient;

        /// <summary>
        /// Static initialization for this module.
        /// </summary>
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
                var logRepository = LogManager.GetRepository(typeof(MainLogger).Assembly);

                var configFile = new FileInfo(Path.Combine(config.LogRootDirectory, LoggerFilePath));

                XmlConfigurator.ConfigureAndWatch(logRepository, configFile);
                
                _log4NetLogger = LogManager.GetLogger(typeof(MainLogger));

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

            this.LogError($"Exception was handled. (ExceptionMessage: {exception.Message}, " +
                          $"ExceptionType: {string.Join(", ", list.Select(x => x.GetType().Name))}) " +
                          "View the Sentry entry for more details.");

            var detailedExceptions = list.Where(x => x is DetailedLogException)
                                         .Cast<DetailedLogException>().ToList();

            var extra = new Dictionary<string, object>();
            
            foreach (var detailedException in detailedExceptions)
            {
                foreach (var pair in detailedException.Context)
                {
                    extra[pair.Key] = pair.Value;
                }                
            }
            
            return _ravenClient.CaptureAsync(new SentryEvent(exception)
            {
                Level = ErrorLevel.Error,
                Extra = extra
            });
        }

        public void LogErrorSync(Exception exception) => 
            this.LogError(exception).GetAwaiter().GetResult();

        /// <summary>
        /// Walks the exception tree and produces all Exception objects in it.
        /// </summary>
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

    /// <summary>
    /// The configuration passed on logger module initialization.
    /// </summary>
    public class LoggerConfigModel
    {
        /// <summary>
        /// The directory where all of the config files are located.
        /// </summary>
        public string LogRootDirectory { get; set; }

        /// <summary>
        /// The DNS used to connect to the Sentry error tracking system.
        /// </summary>
        public string SentryDsn { get; set; }

        /// <summary>
        /// Should the full stacks be printed in the logs when errors occur.
        /// </summary>
        public bool FullStackTracesInLogs { get; set; }
    }

    /// <summary>
    /// An Exception type that carries a context
    /// that gets `json` serialized and sent to the Sentry server.
    /// </summary>
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

        /// <summary>
        /// Make sure that the values are `json` serializable.
        /// </summary>
        public Dictionary<string, object> Context { get; }
    }
}