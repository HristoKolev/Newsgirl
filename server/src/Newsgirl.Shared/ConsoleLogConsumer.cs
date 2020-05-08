namespace Newsgirl.Shared
{
    using System;
    using System.Text.Json;
    using System.Threading.Tasks;

    public class ConsoleLogConsumer<T> : LogConsumerBase<T>
    {
        protected override async ValueTask ProcessBatch(ArraySegment<T> data)
        {
            for (int i = 0; i < data.Count; i++)
            {
                var log = data[i];

                string json = JsonSerializer.Serialize(log);

                await Console.Out.WriteLineAsync(json);
            }
        }

        public ConsoleLogConsumer(ErrorReporter errorReporter) : base(errorReporter)
        {
        }
    }
}
