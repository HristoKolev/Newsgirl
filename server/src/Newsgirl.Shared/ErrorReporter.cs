namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.Json;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Microsoft.Toolkit.HighPerformance.Buffers;
    using Sentry;

    public interface ErrorReporter
    {
        Task<string> Error(Exception exception, string explicitFingerprint, Dictionary<string, object> additionalInfo);

        Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo);

        Task<string> Error(Exception exception, string explicitFingerprint);

        Task<string> Error(Exception exception);

        /// <summary>
        /// For testing purposes.
        /// </summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        void SetInnerReporter(ErrorReporter errorReporter);

        void AddDataHook(Func<Dictionary<string, object>> hook);
    }

    public class ErrorReporterImpl : ErrorReporter
    {
        private readonly ErrorReporterImplConfig config;
        private static readonly TimeSpan SentryFlushTimeout = TimeSpan.FromSeconds(60);
        private static readonly Regex GiudRegex = new Regex("[0-9A-f]{8}(-[0-9A-f]{4}){3}-[0-9A-f]{12}", RegexOptions.Compiled);

        private readonly AsyncLock sentryFlushLock;
        private readonly ISentryClient sentryClient;
        private readonly List<Func<Dictionary<string, object>>> dataHooks;

        public ErrorReporterImpl(ErrorReporterImplConfig config)
        {
            this.config = config;
            this.sentryFlushLock = new AsyncLock();
            this.sentryClient = new SentryClient(new SentryOptions
            {
                AttachStacktrace = true,
                Dsn = config.SentryDsn,
                Release = config.AppVersion,
                Debug = config.SentryDebugMode,
            });
            this.dataHooks = new List<Func<Dictionary<string, object>>>();
        }

        public async Task<string> Error(Exception exception, string explicitFingerprint, Dictionary<string, object> additionalInfo)
        {
            try
            {
                await Console.Error.WriteLineAsync(exception.ToString());
                return await this.SendToSentry(exception, additionalInfo, explicitFingerprint);
            }
            catch (Exception err)
            {
                await Console.Error.WriteLineAsync("An error occured while trying to report an error to Sentry.");
                await Console.Error.WriteLineAsync(err.ToString());
                return null;
            }
        }

        public Task<string> Error(Exception exception, Dictionary<string, object> additionalInfo)
        {
            return this.Error(exception, null, additionalInfo);
        }

        public Task<string> Error(Exception exception, string explicitFingerprint)
        {
            return this.Error(exception, explicitFingerprint, null);
        }

        public Task<string> Error(Exception exception)
        {
            return this.Error(exception, null, null);
        }

        public void SetInnerReporter(ErrorReporter errorReporter) { }

        public void AddDataHook(Func<Dictionary<string, object>> hook)
        {
            this.dataHooks.Add(hook);
        }

        private async Task<string> SendToSentry(Exception exception, Dictionary<string, object> additionalInfo, string explicitFingerprint)
        {
            var sentryEvent = new SentryEvent(exception)
            {
                Level = SentryLevel.Error,
                ServerName = this.config.InstanceName,
                Environment = this.config.Environment,
            };

            var exceptionChain = GetExceptionChain(exception);

            // Set extras form the exception. 
            foreach (var ex in exceptionChain)
            {
                if (ex is DetailedException dex)
                {
                    ApplyExtras(sentryEvent, dex.Details);
                }
            }

            // Set extras from parameter.
            ApplyExtras(sentryEvent, additionalInfo);

            // Set extras from hooks.
            foreach (var hook in this.dataHooks)
            {
                ApplyExtras(sentryEvent, hook.Invoke());
            }

            // Set the fingerprint.
            sentryEvent.SetFingerprint(GetFingerprint(exceptionChain, explicitFingerprint));

            TrimEvent(sentryEvent);

            var eventId = this.sentryClient.CaptureEvent(sentryEvent);

            using (await this.sentryFlushLock.Lock())
            {
                await this.sentryClient.FlushAsync(SentryFlushTimeout);
            }

            return eventId.ToString();
        }

        /// <summary>
        /// Reduce the event's size if necessary.
        /// Sentry has a 1MB/event limit and if the event exceeds that size - it will be dropped.
        /// Currently there is no way of detecting when an event is dropped. The server doesn't register it as an error,
        /// also `Sentry` package doesn't have a way of handling network/protocol errors.
        /// </summary>
        private static void TrimEvent(SentryEvent sentryEvent)
        {
            // The keys of the `Extra` dictionary in order of the size of the JSON representation of the value.
            var keysInOrderOfSize = new Queue<string>(sentryEvent.Extra
                .OrderByDescending(pair => JsonHelper.GetJsonSize(pair.Value))
                .Select(x => x.Key)
                .ToList());

            const int MAX_EVENT_SIZE = 1_000_000;

            // Replace elements from the `Extra` dictionary until the event size is below the threshold.  
            while (GetEventSize(sentryEvent) >= MAX_EVENT_SIZE)
            {
                if (keysInOrderOfSize.TryDequeue(out string key))
                {
                    sentryEvent.SetExtra(key, "__OBJECT_TOO_LARGE__");
                }
                else
                {
                    // Throw if there are no `Extra` elements left.
                    // This means that somehow the event is over the threshold while having no extras.
                    throw new DetailedException("The sentry event is too large even after the `Extra` dictionary was trimmed.");
                }
            }
        }

        private static int GetEventSize(SentryEvent sentryEvent)
        {
            using (var arrayPoolBufferWriter = new ArrayPoolBufferWriter<byte>())
            {
                using (var utf8JsonWriter = new Utf8JsonWriter(arrayPoolBufferWriter))
                {
                    sentryEvent.WriteTo(utf8JsonWriter, null);
                }

                // Dispose the `Utf8JsonWriter` in order to flush before accessing this.
                return arrayPoolBufferWriter.WrittenCount;
            }
        }

        private static IEnumerable<string> GetFingerprint(IReadOnlyList<Exception> exceptions, string explicitFingerprint)
        {
            if (!string.IsNullOrWhiteSpace(explicitFingerprint))
            {
                return new[] { explicitFingerprint };
            }

            string exceptionFingerprint = null;
            foreach (var ex in exceptions)
            {
                if (ex is DetailedException dex && !string.IsNullOrWhiteSpace(dex.Fingerprint))
                {
                    exceptionFingerprint = dex.Fingerprint;
                }
            }

            if (!string.IsNullOrWhiteSpace(exceptionFingerprint))
            {
                return new[] { exceptionFingerprint };
            }

            return exceptions.Select(GetFingerprint).ToArray();
        }

        private static void ApplyExtras(IHasExtra sentryEvent, Dictionary<string, object> data)
        {
            if (data == null)
            {
                return;
            }

            sentryEvent.SetExtras(data);
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

            // Reverse the list so that the exceptions are in most inner to most outer order.
            list.Reverse();

            return list;
        }

        private static string GetFingerprint(Exception exception)
        {
            static (string, string) GetMethodData(StackFrame stackFrame)
            {
                var method = stackFrame.GetMethod();

                if (method == null)
                {
                    return (null, null);
                }

                return (method.DeclaringType?.FullName ?? "(unknown)", method.Name);
            }

            var methodsData = new StackTrace(exception)
                .GetFrames()
                .Select(GetMethodData)
                .Where(x => !string.IsNullOrWhiteSpace(x.Item1) && !string.IsNullOrWhiteSpace(x.Item2))
                .Reverse()
                .ToList();

            string frames = string.Join("\n", methodsData.Select(x => $"{x.Item1} => {x.Item2}"));
            frames = GiudRegex.Replace(frames, "00000000-0000-0000-0000-000000000000");
            return $"[{exception.GetType()}]\n{frames}";
        }
    }

    public class ErrorReporterImplConfig
    {
        public string SentryDsn { get; set; }

        public bool SentryDebugMode { get; set; }

        public string InstanceName { get; set; }

        public string Environment { get; set; }

        public string AppVersion { get; set; }
    }

    public class DetailedException : Exception
    {
        public DetailedException()
        {
            this.Details = new Dictionary<string, object>();
        }

        public DetailedException(string message)
            : base(message)
        {
            this.Details = new Dictionary<string, object>();
        }

        public DetailedException(string message, Exception inner)
            : base(message, inner)
        {
            this.Details = new Dictionary<string, object>();
        }

        // ReSharper disable once CollectionNeverUpdated.Global
        public Dictionary<string, object> Details { get; }

        public string Fingerprint { get; set; }
    }
}
