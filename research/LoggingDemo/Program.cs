namespace LoggingDemo
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Text.Json;
    using System.Threading.Tasks;

    internal static class Program
    {
        private static async Task Main(string[] args)
        {
            var log = new StructuredLogger(builder =>
            {
                builder.AddConfig("log_data", new LogConsumer<LogData>[]
                {
                    new LogDataConsoleConsumer(), 
                });
                
                builder.SetEnabled(new string[0]);
            });

            log.SetEnabled(new []{"log_data"});
            
            for (int i = 0; i < 100; i++)
            {
                log.Log("log_data", () => new LogData("Here cats are good.")
                {
                    {"key", "val"}
                });   
            }
            
            await log.DisposeAsync();
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
