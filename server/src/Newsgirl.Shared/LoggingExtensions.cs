namespace Newsgirl.Shared
{
    using System;
    using System.Collections;
    using System.Collections.Generic;

    public static class GeneralLoggingExtensions
    {
        public const string GeneralKey = "GENERAL_LOG";

        public static void General(this ILog log, Func<LogData> func) => log.Log(GeneralKey, func);
    }
    
    public class LogData : IEnumerable
    {
        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

        public LogData(string message) => this.Fields.Add("message", message);

        public void Add(string key, object val) => this.Fields.Add(key, val);

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }
}
