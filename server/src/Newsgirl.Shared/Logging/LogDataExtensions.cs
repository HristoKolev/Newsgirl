namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    /// <summary>
    /// Using this extension method allows us to not specify the stream name and the event data structure.
    /// </summary>
    public static class GeneralLoggingExtensions
    {
        public const string GeneralEventStream = "GENERAL_LOG";

        public static void General(this ILog log, Func<LogData> func) => log.Log(GeneralEventStream, func);
    }
    
    /// <summary>
    /// This is used as a most general log data structure.
    /// </summary>
    public class LogData : IEnumerable
    {
        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

        public LogData(string message)
        {
            this.Fields.Add("message", message);
            this.Fields.Add("log_date", DateTime.UtcNow.ToString("O"));
        }

        /// <summary>
        /// This is not meant to be used explicitly, but with he collection initialization syntax.
        /// </summary>
        public void Add(string key, object val) => this.Fields.Add(key, val);

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();

        public static implicit operator LogData(string x) => new LogData(x);
    }
}
