namespace Newsgirl.Shared.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading;
    using System.Threading.Tasks;
    using Elasticsearch.Net;
    using Sentry;
    using Sentry.Protocol;

    public class CustomLogger : ILog
    {
        private readonly CustomLoggerConfig config;
        private readonly AsyncLock sentryFlushLock;
        private readonly TimeSpan sentryFlushTimeout;
        private ISentryClient sentryClient;
        private static readonly Regex GiudRegex = new Regex("[0-9A-f]{8}(-[0-9A-f]{4}){3}-[0-9A-f]{12}", RegexOptions.Compiled);
        private readonly List<AsyncLocal<Func<Dictionary<string, object>>>> syncErrorDataHooks 
            = new List<AsyncLocal<Func<Dictionary<string, object>>>>();

        private ElasticLowLevelClient elasticsearchClient;

        public CustomLogger(CustomLoggerConfig config)
        {
            this.config = config;
            this.sentryFlushLock = new AsyncLock();
            this.sentryFlushTimeout = TimeSpan.FromSeconds(60);

            if (!this.config.DisableSentryIntegration)
            {
                this.CreateSentryClient();
            }

            if (!this.config.DisableElasticsearchIntegration)
            {
                this.CreateElasticsearchClient();
            }
        }

        private void CreateElasticsearchClient()
        {
            if (this.elasticsearchClient != null)
            {
                return;
            }

            var elasticConnectionConfiguration = new ConnectionConfiguration(new Uri(this.config.ElasticsearchConfig.Url));
            elasticConnectionConfiguration.BasicAuthentication(this.config.ElasticsearchConfig.Username, this.config.ElasticsearchConfig.Password);
            this.elasticsearchClient = new ElasticLowLevelClient(elasticConnectionConfiguration);
        }

        public Task Debug(string message)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            return this.Log(message, null);
        }

        public Task Debug(string message, Dictionary<string, object> fields)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            return this.Log(message, fields);
        }

        public Task Debug(Func<string> func)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            return this.Log(func(), null);
        }

        public Task Debug(Func<(string, Dictionary<string, object>)> func)
        {
            if (!this.config.EnableDebug)
            {
                return Task.CompletedTask;
            }

            var (message, fields) = func();
            
            return this.Log(message, fields);
        }

        public Task Log(string message)
        {
            return this.Log(message, null);
        }

        public async Task Log(string message, Dictionary<string, object> fields)
        {
            if (!this.config.DisableConsoleLogging)
            {
                await Console.Out.WriteLineAsync(message);
            }
            
            fields ??= new Dictionary<string, object>();
            fields.Add("message", message);

            await this.SendToElasticsearch(fields);
        }

        public async Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo)
        {
            try
            {
                if (!this.config.DisableConsoleLogging)
                {
                    await Console.Error.WriteLineAsync(exception.ToString());
                }

                if (!this.config.DisableSentryIntegration)
                {
                    return await this.SendToSentry(exception, additionalInfo, fingerprint);
                }

                return null;
            }
            catch (Exception err)
            {
                if (!this.config.DisableConsoleLogging)
                {
                    await Console.Error.WriteLineAsync(err.ToString());
                }

#if DEBUG
                throw;
#endif

#pragma warning disable 162
                return null;
#pragma warning restore 162
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

        private async Task SendToElasticsearch(Dictionary<string, object> fields)
        {
            this.CreateElasticsearchClient();
            
            fields.Add("log_date", DateTime.UtcNow.ToString("O"));
            
            string jsonBody = JsonSerializer.Serialize(fields);
            
            var response = await this.elasticsearchClient.IndexAsync<CustomElasticsearchResponse>(this.config.ElasticsearchConfig.IndexName, jsonBody);

            if (!response.Success)
            {
                throw new ApplicationException(response.ToString());
            }
        }
        
        private void CreateSentryClient()
        {
            this.sentryClient ??= new SentryClient(new SentryOptions
            {
                AttachStacktrace = true,
                Dsn = new Dsn(this.config.SentryDsn),
                Release = this.config.Release
            });
        }

        private async Task<string> SendToSentry(
            Exception exception,
            Dictionary<string, object> additionalInfo,
            string explicitlyDefinedFingerprint)
        {
            this.CreateSentryClient();

            var sentryEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Error
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
                        SetExtra(sentryEvent, kvp);
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
                    SetExtra(sentryEvent, kvp);
                }
            }

            foreach (var hook in this.syncErrorDataHooks)
            {
                var data = hook.Value?.Invoke();

                if (data != null)
                {
                    foreach (var kvp in data)
                    {
                        SetExtra(sentryEvent, kvp);
                    }
                }
            }

            if (!string.IsNullOrWhiteSpace(explicitlyDefinedFingerprint))
            {
                sentryEvent.SetFingerprint(new []{explicitlyDefinedFingerprint});
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

        private static void SetExtra(SentryEvent sentryEvent, KeyValuePair<string, object> kvp)
        {
            sentryEvent.SetExtra(kvp.Key, kvp.Value);
        }

        /// <summary>
        ///     Returns a flat list of all the exceptions down the .InnerException chain.
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

        public void AddSyncHook(AsyncLocal<Func<Dictionary<string,object>>> hook)
        {
            this.syncErrorDataHooks.Add(hook);
        }
    }

    public interface ILog
    {
        Task Debug(string message);
        
        Task Debug(string message, Dictionary<string, object> fields);

        Task Debug(Func<string> func);
        
        Task Debug(Func<(string, Dictionary<string, object>)> func);

        Task Log(string message);
        
        Task Log(string message, Dictionary<string, object> fields);

        Task<string> Error(Exception exception, string fingerprint, Dictionary<string, object> additionalInfo);
        
        Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo);
        
        Task<string> Error(Exception exception, string fingerprint);
        
        Task<string> Error(Exception exception);
    }

    // ReSharper disable once ClassNeverInstantiated.Global
    public class CustomLoggerConfig
    {
        public bool DisableSentryIntegration { get; set; }

        public bool DisableConsoleLogging { get; set; }

        public bool EnableDebug { get; set; }

        public string ServerName { get; set; }

        public string Environment { get; set; }

        public string SentryDsn { get; set; }

        public string Release { get; set; }
        
        public bool DisableElasticsearchIntegration { get; set; }

        public ElasticsearchConfig ElasticsearchConfig { get; set; }
    }

    /// <summary>
    ///     Stolen from Sentry's source code.
    /// </summary>
    internal static class SentryStackTraceFactory
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
            ("System.Threading.Tasks.ValueTask`1", "get_Result")
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
            const string UNKNOWN_REQUIRED_FIELD = "(unknown)";

            var frame = new SentryStackFrame();

            // ReSharper disable once PatternAlwaysOfType
            if (stackFrame.GetMethod() is MethodBase method)
            {
                // TODO: SentryStackFrame.TryParse and skip frame instead of these unknown values:
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
    
    public class ElasticsearchConfig
    {
        public string Url { get; set; }

        public string Username { get; set; }

        public string Password { get; set; }
        
        public string IndexName { get; set; }
    }
    
    public class CustomElasticsearchResponse : ElasticsearchResponseBase
    {
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
