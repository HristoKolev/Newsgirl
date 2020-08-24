namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Sentry;
    using Sentry.Protocol;

    public interface ErrorReporter
    {
        Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo);

        Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo);

        Task<string> Error(Exception exception, string fingerprint);

        Task<string> Error(Exception exception);
    }

    public class ErrorReporterImpl : ErrorReporter
    {
        private readonly ErrorReporterConfig config;
        private readonly AsyncLock sentryFlushLock;
        private readonly TimeSpan sentryFlushTimeout;
        private readonly ISentryClient sentryClient;
        private static readonly Regex GiudRegex = new Regex("[0-9A-f]{8}(-[0-9A-f]{4}){3}-[0-9A-f]{12}", RegexOptions.Compiled);
        private readonly List<AsyncLocal<Func<Dictionary<string, object>>>> syncErrorDataHooks = new List<AsyncLocal<Func<Dictionary<string, object>>>>();

        public ErrorReporterImpl(ErrorReporterConfig config)
        {
            this.config = config;
            this.sentryFlushLock = new AsyncLock();
            this.sentryFlushTimeout = TimeSpan.FromSeconds(60);
            this.sentryClient = new SentryClient(new SentryOptions
            {
                AttachStacktrace = true,
                Dsn = new Dsn(this.config.SentryDsn),
                Release = this.config.Release,
            });
        }

        public async Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo)
        {
            try
            {
                await Console.Error.WriteLineAsync(exception.ToString());

                return await this.SendToSentry(exception, additionalInfo, fingerprint);
            }
            catch (Exception err)
            {
                await Console.Error.WriteLineAsync(err.ToString());
#if DEBUG
                throw;
#else
                return null;
#endif
            }
        }

        public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
        {
            return this.Error(exception, null, additionalInfo);
        }

        public Task<string> Error(Exception exception, string fingerprint)
        {
            return this.Error(exception, fingerprint, null);
        }

        public Task<string> Error(Exception exception)
        {
            return this.Error(exception, null, null);
        }

        private async Task<string> SendToSentry(Exception exception, Dictionary<string, object> additionalInfo, string explicitlyDefinedFingerprint)
        {
            var sentryEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Error,
            };

            if (!string.IsNullOrWhiteSpace(this.config.ServerName))
            {
                sentryEvent.ServerName = this.config.ServerName;
            }

            if (!string.IsNullOrWhiteSpace(this.config.Environment))
            {
                sentryEvent.Environment = this.config.Environment;
            }

            string[] fingerprintFromExceptionProperty = null;

            var exceptionChain = GetExceptionChain(exception);

            for (int i = 0; i < exceptionChain.Count; i++)
            {
                var current = exceptionChain[i];

                if (current is DetailedLogException detailedLogException)
                {
                    foreach (var kvp in detailedLogException.Details)
                    {
                        sentryEvent.SetExtra(kvp.Key, kvp.Value);
                    }

                    if (!string.IsNullOrWhiteSpace(detailedLogException.Fingerprint))
                    {
                        fingerprintFromExceptionProperty = new[] {detailedLogException.Fingerprint};
                    }
                }
            }

            if (additionalInfo != null)
            {
                foreach (var kvp in additionalInfo)
                {
                    sentryEvent.SetExtra(kvp.Key, kvp.Value);
                }
            }

            foreach (var hook in this.syncErrorDataHooks)
            {
                var data = hook.Value?.Invoke();

                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        sentryEvent.SetExtra(kvp.Key, kvp.Value);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(explicitlyDefinedFingerprint))
            {
                sentryEvent.SetFingerprint(new[] {explicitlyDefinedFingerprint});
            }
            else if (fingerprintFromExceptionProperty != null)
            {
                sentryEvent.SetFingerprint(fingerprintFromExceptionProperty);
            }
            else
            {
                var fingerprints = new string[exceptionChain.Count];

                for (int i = 0; i < exceptionChain.Count; i++)
                {
                    var current = exceptionChain[i];

                    fingerprints[i] = GetFingerprint(current);
                }

                sentryEvent.SetFingerprint(fingerprints);
            }

            var eventId = this.sentryClient.CaptureEvent(sentryEvent);

            using (await this.sentryFlushLock.Lock())
            {
                await this.sentryClient.FlushAsync(this.sentryFlushTimeout);
            }

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

        private static string GetFingerprint(Exception exception)
        {
            var stackTrace = SentryStackTraceFactory.Create(exception);

            string frames = string.Join("\n", stackTrace.Frames.Select(frame => $"{frame.Module} => {frame.Function}"));

            frames = GiudRegex.Replace(frames, "00000000-0000-0000-0000-000000000000");

            return $"[{exception.GetType()}]\n{frames}";
        }

        public void AddSyncHook(AsyncLocal<Func<Dictionary<string, object>>> hook)
        {
            this.syncErrorDataHooks.Add(hook);
        }

        /// <summary>
        /// Stolen from Sentry's source code.
        /// </summary>
        private static class SentryStackTraceFactory
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

                bool firstFrames = true;
                foreach (var stackFrame in frames)
                {
                    // Remove the frames until the call for capture with the SDK
                    if (firstFrames
                        // ReSharper disable once PatternAlwaysOfType
                        && stackFrame?.GetMethod() is MethodBase method
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
                const string UNKNOWN_REQUIRED_FIELD = "(unknown)";

                var frame = new SentryStackFrame();

                // ReSharper disable once PatternAlwaysOfType
                if (stackFrame.GetMethod() is MethodBase method)
                {
                    frame.Module = method.DeclaringType?.FullName ?? UNKNOWN_REQUIRED_FIELD;
                    frame.Package = method.DeclaringType?.Assembly.FullName;
                    frame.Function = method.Name;
                }

                frame.InApp = true;
                frame.FileName = stackFrame.GetFileName();

                // stackFrame.HasILOffset() throws NotImplemented on Mono 5.12
                int ilOffset = stackFrame.GetILOffset();

                if (ilOffset != 0)
                {
                    frame.InstructionOffset = stackFrame.GetILOffset();
                }

                int lineNo = stackFrame.GetFileLineNumber();
                if (lineNo != 0)
                {
                    frame.LineNumber = lineNo;
                }

                int colNo = stackFrame.GetFileColumnNumber();

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
    }

    public class ErrorReporterConfig
    {
        public string ServerName { get; set; }

        public string Environment { get; set; }

        public string SentryDsn { get; set; }

        public string Release { get; set; }
    }

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
}
