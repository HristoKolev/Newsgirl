using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using log4net.Config;
using Sentry;
using Sentry.Protocol;

namespace Newsgirl.Shared.Infrastructure
{
    public static class MainLogger
    {
        private static readonly object SyncLock = new object();

        private static readonly List<Func<(string id, Dictionary<string, string> info)?>> UserHooks
            = new List<Func<(string id, Dictionary<string, string> info)?>>();

        private static bool IsInitialized;

        private static ILog Log4NetLogger;

        private static IDisposable SentryHandle;

        private static LoggerConfigModel LoggerConfig;

        private static string ServerName;

        private static readonly List<string> Logs = new List<string>();

        private static bool DebugLogging;

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static IDisposable Initialize(LoggerConfigModel config)
        {
            if (IsInitialized)
            {
                return null;
            }

            lock (SyncLock)
            {
                if (IsInitialized)
                {
                    return null;
                }

 

                SentryHandle = SentrySdk.Init(x =>
                {
                    x.AttachStacktrace = true;
                    x.Dsn = new Dsn(config.SentryDsn);
                });

                LoggerConfig = config;

                IsInitialized = true;

                DebugLogging = config.DebugLogging;
            }

            return new DisposableFunction(() => SentryHandle?.Dispose());
        }

      

        public static void Print(string message)
        {
            Log4NetLogger?.Debug(message);

            if (LoggerConfig?.InMemoryLogs != null)
            {
                Logs.Add(message);
            }
        }

        public static void Debug(string message)
        {
            if (DebugLogging)
            {
                Print(message);
            }
        }
 
        public static string Error(Exception exception, Dictionary<string, object> additionalInfo = null)
        {
            Log4NetLogger.Error(exception.Message, exception);

            
        }

    }

    public class LoggerConfigModel
    {
        public string SentryDsn { get; set; }

        public string ConfigDirectory { get; set; }

        public string Environment { get; set; }

        public bool InMemoryLogs { get; set; }

        public bool DisableConsoleLogging { get; set; }

        public bool DebugLogging { get; set; }
    }

    // ReSharper disable once ClassNeverInstantiated.Global


    

#pragma warning disable CA1063 // Implement IDisposable Correctly
    public class DisposableFunction : IDisposable
#pragma warning restore CA1063 // Implement IDisposable Correctly
    {
        private readonly Action onDispose;

        public DisposableFunction(Action onDispose)
        {
            this.onDispose = onDispose;
        }

        void IDisposable.Dispose()
        {
            this.onDispose();
        }
    }
}