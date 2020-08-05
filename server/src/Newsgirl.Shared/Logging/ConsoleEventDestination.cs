namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ConsoleEventDestination : EventDestination<LogData>
    {
        public ConsoleEventDestination(ErrorReporter errorReporter) : base(errorReporter)
        {
        }
        
        protected override async ValueTask Flush(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var log = data[i];

                string json = JsonSerializer.Serialize(log.Fields);

                await Console.Out.WriteLineAsync(json);
            }
        }
    }
}
