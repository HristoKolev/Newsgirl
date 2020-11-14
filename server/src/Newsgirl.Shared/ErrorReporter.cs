namespace Newsgirl.Shared
{
    using System;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Diagnostics;
    using System.Linq;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using Sentry;
    using Sentry.Protocol;

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
        private static readonly TimeSpan SentryFlushTimeout = TimeSpan.FromSeconds(60);
        private static readonly Regex GiudRegex = new Regex("[0-9A-f]{8}(-[0-9A-f]{4}){3}-[0-9A-f]{12}", RegexOptions.Compiled);

        private readonly ErrorReporterConfig config;
        private readonly ISentryClient sentryClient;
        private readonly AsyncLock sentryFlushLock;
        private readonly List<Func<Dictionary<string, object>>> dataHooks;

        public ErrorReporterImpl(ErrorReporterConfig config)
        {
            this.config = config;
            this.sentryFlushLock = new AsyncLock();
            this.dataHooks = new List<Func<Dictionary<string, object>>>();
            this.sentryClient = new SentryClient(new SentryOptions
            {
                AttachStacktrace = true,
                Dsn = new Dsn(this.config.SentryDsn),
                Release = this.config.Release,
            });
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
                ServerName = this.config.ServerName,
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

            var eventId = this.sentryClient.CaptureEvent(sentryEvent);

            using (await this.sentryFlushLock.Lock())
            {
                await this.sentryClient.FlushAsync(SentryFlushTimeout);
            }

            return eventId.ToString();
        }

        private static IEnumerable<string> GetFingerprint(IReadOnlyList<Exception> exceptions, string explicitFingerprint)
        {
            if (!string.IsNullOrWhiteSpace(explicitFingerprint))
            {
                return new[] {explicitFingerprint};
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
                return new[] {exceptionFingerprint};
            }

            return exceptions.Select(GetFingerprint).ToArray();
        }

        private static void ApplyExtras(BaseScope sentryEvent, Dictionary<string, object> data)
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

    public class ErrorReporterConfig
    {
        public string ServerName { get; set; }

        public string Environment { get; set; }

        public string SentryDsn { get; set; }

        public string Release { get; set; }
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
