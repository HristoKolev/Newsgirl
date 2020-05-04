namespace LoggingDemo
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Program
    {
        static async Task Main(string[] args)
        {
            var destinations = new LogConsumer<LogData>[]
            {
                new LogDataConsoleConsumer(),
            };

            await using (var log = new StructuredLogger<LogData>(destinations))
            {
                log.CurrentLevel = LogLevel.Warn;
                
                for (int i = 0; i < 100; i++)
                {
                    log.Warn(x => new LogData("Here cats are good.")
                    {
                        {"key", "val"}
                    });   
                }
            }
        }
    }

    public class LogData : IEnumerable
    {
        public Dictionary<string, object> Fields { get; } = new Dictionary<string, object>();

        public LogData(string message) => this.Fields.Add("message", message);

        public void Add(string key, object val) => this.Fields.Add(key, val);

        IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
    }

    public class LogDataConsoleConsumer : LogConsumerBase<LogData>
    {
        protected override async ValueTask ProcessBatch(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var logData = data[i];
                
                await Console.Out.WriteLineAsync(JsonSerializer.Serialize(logData.Fields));    
            }
        }
    }
}
