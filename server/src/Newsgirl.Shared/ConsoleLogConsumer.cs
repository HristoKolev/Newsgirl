namespace Newsgirl.Shared
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ConsoleLogDataConsumer : LogConsumer<LogData>
    {
        protected override async ValueTask Flush(ArraySegment<LogData> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var log = data[i];

                string json = JsonSerializer.Serialize(log.Fields);

                await Console.Out.WriteLineAsync(json);
            }
        }

        public ConsoleLogDataConsumer(ErrorReporter errorReporter) : base(errorReporter)
        {
        }
    }
}
