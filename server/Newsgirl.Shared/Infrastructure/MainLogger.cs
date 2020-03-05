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

                ConfigureLog4Net(config);

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

        private static void ConfigureLog4Net(LoggerConfigModel config)
        {
            // Configure log4net.
            var assembly = Assembly.GetEntryAssembly();
            var logRepository = LogManager.GetRepository(assembly);

            string log4NetConfigContents = $@"<?xml version=""1.0"" encoding=""utf-8""?>
                <log4net>
                  <appender name=""MyFileAppender"" type=""log4net.Appender.RollingFileAppender"">
                    <file value=""{config.ConfigDirectory.TrimEnd('/').TrimEnd('\\')}/logs/{Assembly.GetEntryAssembly().GetName().Name}.txt"" />
                    <appendToFile value=""true"" />
                    <rollingStyle value=""Size"" />
                    <staticLogFileName value=""true"" />
                    <maximumFileSize value=""10MB"" />
                    <maxSizeRollBackups value=""100"" />
                    <encoding value=""utf-8"" />
                    <lockingModel type=""log4net.Appender.FileAppender+MinimalLock"" />
                    <layout type=""log4net.Layout.PatternLayout"">
                      <conversionPattern value=""%date | THR%thread | %message%newline"" />
                    </layout>
                  </appender>

                  <appender name=""ConsoleOutAppender"" type=""log4net.Appender.ConsoleAppender"">
                    <filter type=""log4net.Filter.LevelRangeFilter"">
                      <levelMin value=""DEBUG"" />
                      <levelMax value=""WARN"" />
                    </filter>
                    <layout type=""log4net.Layout.PatternLayout"">
                      <conversionPattern value=""%message%newline""  />
                    </layout>
                  </appender>

                  <appender name=""ConsoleErrorAppender"" type=""log4net.Appender.ConsoleAppender"">
                    <filter type=""log4net.Filter.LevelRangeFilter"">
                      <levelMin value=""ERROR"" />
                      <levelMax value=""FATAL"" />
                    </filter>
                    <target value=""Console.Error"" />
                    <layout type=""log4net.Layout.PatternLayout"">
                      <conversionPattern value=""%message%newline"" />
                    </layout>
                  </appender>

                  <root>
                    <level value=""ALL"" />
                    <appender-ref ref=""MyFileAppender"" />
                    {(!config.DisableConsoleLogging ? @"<appender-ref ref=""ConsoleOutAppender"" /><appender-ref ref=""ConsoleErrorAppender"" />" : "")}
                  </root>
                </log4net>
            ";

            using (var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(log4NetConfigContents)))
            {
                XmlConfigurator.Configure(logRepository, memoryStream);
                Log4NetLogger = LogManager.GetLogger(assembly, "Global logger");
            }
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetDebugLogging(bool value)
        {
            DebugLogging = value;
        }

        public static string Error(Exception exception, Dictionary<string, object> additionalInfo = null)
        {
            Log4NetLogger.Error(exception.Message, exception);

            var exceptionList = GetExceptionChain(exception);

            var contextEntries = exceptionList.Where(x => x is DetailedLogException)
                                     .Cast<DetailedLogException>()
                                     .SelectMany(x => x.Details)
                                     .ToList();

            if (additionalInfo != null)
            {
                contextEntries.AddRange(additionalInfo);
            }

            var sentryEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Error,
            };

            foreach (var entry in contextEntries)
            {
                sentryEvent.SetExtra(entry.Key, entry.Value);
            }

            string customFingerprint = exceptionList.Where(x => x is DetailedLogException)
                                                   .Cast<DetailedLogException>()
                                                   .Select(x => x.Fingerprint)
                                                   .FirstOrDefault(x => !string.IsNullOrWhiteSpace(x));
            if (customFingerprint != null)
            {
                sentryEvent.SetFingerprint(new[] { customFingerprint });
            }
            else
            {
                sentryEvent.SetFingerprint(exceptionList.Select(GetFingerprint));
            }

            var sentryUser = new User();

            foreach (var userHook in UserHooks)
            {
                var userData = userHook();

                if (userData == null)
                {
                    continue;
                }

                sentryUser.Id = userData.Value.id;
                sentryUser.Other = userData.Value.info;

                break;
            }

            sentryEvent.User = sentryUser;

            sentryEvent.ServerName = ServerName;

            if (!string.IsNullOrWhiteSpace(LoggerConfig.Environment))
            {
                sentryEvent.Environment = LoggerConfig.Environment;
            }

            var eventId = SentrySdk.CaptureEvent(sentryEvent);

            return eventId.ToString();
        }

        /// <summary>
        /// Returns a flat list of all the exceptions down the .InnerException chain.
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

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static List<string> GetLogs()
        {
            return Logs.ToList();
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void AddGetUser(Func<(string id, Dictionary<string, string> info)?> func)
        {
            UserHooks.Add(func);
        }

        private static string GetFingerprint(Exception exception)
        {
            var stackTrace = SentryStackTraceFactory.Create(exception);

            string frames = string.Join("\n", stackTrace.Frames.Select(frame => $"{frame.Module} => {frame.Function}"));

            return $"[{exception.GetType()}]\n{frames}";
        }

        [EditorBrowsable(EditorBrowsableState.Never)]
        public static void SetServerName(string serverName)
        {
            ServerName = serverName;
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
    public class DetailedLogException : Exception
    {
        public DetailedLogException()
        {
            this.Details = new Dictionary<string, object>();
        }

        public DetailedLogException(string message)
            : base(message)
        {
            this.Details = new Dictionary<string, object>();
        }

        public DetailedLogException(string message, Exception inner)
            : base(message, inner)
        {
            this.Details = new Dictionary<string, object>();
        }

        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, object> Details { get; }
        public string Fingerprint { get; set; }
    }

    /// <summary>
    /// Откраднато от модула на Sentry.
    /// </summary>
    public static class SentryStackTraceFactory
    {
        private static readonly List<(string, string)> IgnoredFrames = new List<(string, string)>
        {
            ("System.Runtime.CompilerServices.TaskAwaiter", "GetResult"),
            ("System.Runtime.CompilerServices.TaskAwaiter`1", "GetResult"),

            ("System.Runtime.CompilerServices.TaskAwaiter", "HandleNonSuccessAndDebuggerNotification"),
            ("System.Runtime.CompilerServices.TaskAwaiter`1", "HandleNonSuccessAndDebuggerNotification"),

            ("System.Runtime.CompilerServices.TaskAwaiter", "ThrowForNonSuccess"),
            ("System.Runtime.CompilerServices.TaskAwaiter`1", "ThrowForNonSuccess"),

            ("System.Runtime.ExceptionServices.ExceptionDispatchInfo", "Throw"),

            ("System.Runtime.CompilerServices.ValueTaskAwaiter`1", "GetResult"),
            ("System.Threading.Tasks.ValueTask`1", "get_Result"),
        };

        public static SentryStackTrace Create(Exception exception)
        {
            return Create(new StackTrace(exception, true));
        }

        private static SentryStackTrace Create(StackTrace stackTrace)
        {
            var frames = CreateFrames(stackTrace).Reverse();

            var stacktrace = new SentryStackTrace();

            foreach (var frame in frames)
            {
                if (IgnoredFrames.All(tuple => frame.Module != tuple.Item1 || frame.Function != tuple.Item2))
                {
                    stacktrace.Frames.Add(frame);
                }
            }

            return stacktrace;
        }

        private static IEnumerable<SentryStackFrame> CreateFrames(StackTrace stackTrace)
        {
            var frames = stackTrace?.GetFrames();

            if (frames == null)
            {
                yield break;
            }

            var firstFrames = true;
            foreach (var stackFrame in frames)
            {
                // Remove the frames until the call for capture with the SDK
                if (firstFrames
                    // ReSharper disable once PatternAlwaysOfType
                    && stackFrame.GetMethod() is MethodBase method
                    && method.DeclaringType?.AssemblyQualifiedName?.StartsWith("Sentry") == true)
                {
                    continue;
                }

                firstFrames = false;

                var frame = InternalCreateFrame(stackFrame);
                if (frame != null)
                {
                    yield return frame;
                }
            }
        }

        private static SentryStackFrame InternalCreateFrame(StackFrame stackFrame)
        {
            const string unknownRequiredField = "(unknown)";

            var frame = new SentryStackFrame();

            // ReSharper disable once PatternAlwaysOfType
            if (stackFrame.GetMethod() is MethodBase method)
            {
                // TODO: SentryStackFrame.TryParse and skip frame instead of these unknown values:
                frame.Module = method.DeclaringType?.FullName ?? unknownRequiredField;
                frame.Package = method.DeclaringType?.Assembly.FullName;
                frame.Function = method.Name;
            }

            frame.InApp = true;
            frame.FileName = stackFrame.GetFileName();

            // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
            var ilOffset = stackFrame.GetILOffset();

            if (ilOffset != 0)
            {
                frame.InstructionOffset = stackFrame.GetILOffset();
            }

            var lineNo = stackFrame.GetFileLineNumber();
            if (lineNo != 0)
            {
                frame.LineNumber = lineNo;
            }

            var colNo = stackFrame.GetFileColumnNumber();

            if (lineNo != 0)
            {
                frame.ColumnNumber = colNo;
            }

            DemangleAsyncFunctionName(frame);
            DemangleAnonymousFunction(frame);

            return frame;
        }

        private static void DemangleAsyncFunctionName(SentryStackFrame frame)
        {
            if (frame.Module == null || frame.Function != "MoveNext")
            {
                return;
            }

            //  Search for the function name in angle brackets followed by d__<digits>.
            //
            // Change:
            //   RemotePrinterService+<UpdateNotification>d__24 in MoveNext at line 457:13
            // to:
            //   RemotePrinterService in UpdateNotification at line 457:13

            var match = Regex.Match(frame.Module, @"^(.*)\+<(\w*)>d__\d*$");
            if (match.Success && match.Groups.Count == 3)
            {
                frame.Module = match.Groups[1].Value;
                frame.Function = match.Groups[2].Value;
            }
        }

        private static void DemangleAnonymousFunction(SentryStackFrame frame)
        {
            if (frame?.Function == null)
            {
                return;
            }

            // Search for the function name in angle brackets followed by b__<digits/letters>.
            //
            // Change:
            //   <BeginInvokeAsynchronousActionMethod>b__36
            // to:
            //   BeginInvokeAsynchronousActionMethod { <lambda> }

            var match = Regex.Match(frame.Function, @"^<(\w*)>b__\w+$");
            if (match.Success && match.Groups.Count == 2)
            {
                frame.Function = match.Groups[1].Value + " { <lambda> }";
            }
        }
    }

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