
# The problem

Standard logging frameworks like `log4net`, `serilog` and `NLog` have several problems:

* Lack of first class support for structured logging.

* Log granularity is centered around arbitrary "levels" like `Debug`, `Warning` and `Error`.

* Performance. When using the aforementioned frameworks, blocking work is being done when logging a message. 

# Goals of this module

* To provide an extensible framework for structured logging.

* Allow different event streams to be turned on and off and assigned different destinations at runtime. When an event stream is turned off there should be minimal performance penalty for emitting an event to that stream.

* To allow events to have static types.

# How to use

### Build and use a logger

```c#

var builder = new StructuredLoggerBuilder();

string elkUrl = "http://localhost/path-to-elk";
string elkUsername = "username";
string elkPassword = "password";

builder.AddEventStream("GeneralLogs", new Dictionary<string,Func<EventDestination<GeneralLogData>>>
{
    {"ConsoleEventDestination", () => new ConsoleEventDestination()},
    {"ElasticsearchEventDestination", () => new ElasticsearchEventDestination(elkUrl, elkUsername, elkPassword)},
});

// after you are done defining the event streams you need to create a logger.
var log = builder.Build();

// configure the logger in order to build the underlying optimized implementation.
// usualy the data that gets passed to Reconfigure comes from some config file somewhere.
await log.Reconfigure(new [] {
    new EventStreamConfig
    {
        Name = "GeneralLogs",
        Enabled = true,
        Destinations = new[]
        {
            new EventDestinationConfig
            {
                Name = "ConsoleEventDestination",
                Enabled = true,
            },
            new EventDestinationConfig
            {
                Name = "ElasticsearchEventDestination",
                Enabled = true,
            }
        }
    };
});

// use the logger to dispatch events
log.Log<GeneralLogData>("GeneralLogs", () => new GeneralLogData { ... });

// the above code is a bit wordy. That is why we use extension methods that abstract the event stream name and the TEventData type:

public static class GeneralLoggingExtensions
{
    public const string GeneralEventStream = "GENERAL_LOG";

    public static void General(this Log log, Func<LogData> func)
    {
        log.Log(GeneralEventStream, func);
    }
}

// this is how we use it:
log.General(() => new GeneralLogData { ... });


```

At runtime if the need arises you can call `Reconfigure` again and the underlying implementation will change to reflect the new configuration. This change is atomic and transparent - the work of threads that are actively enqueuing events will not be disrupted. All previously used destination instances will be disposed.  

### Create a custom event destination

In order to create a custom event destination you must extend `EventDestination<TEventData>`.

This example destination takes each item and writes it's json representation to `stdout`. 

```c#

public class ConsoleEventDestination : EventDestination<GeneralLogData>
{
    public ConsoleEventDestination(ErrorReporter errorReporter) : base(errorReporter)
    {
    }
    
    protected override async ValueTask Flush(ArraySegment<GeneralLogData> data)
    {
        for (int i = 0; i < data.Count; i++)
        {
            var item = data[i];

            string json = JsonSerializer.Serialize(item);

            await Console.Out.WriteLineAsync(json);
        }
    }
}

```

Deriving classes must provide a `Flush` implementation that receives an `ArraySegment<TEventData>`. In times of low traffic that segment will be of length 1. If on the other hand during the call to `Flush` new events were enqueued they will be buffered and flushed after the current call to `Flush` returns.

If `Flush` throws an exception, a retry mechanism will call `Flush` again with the same data a number of times with short pauses in between before giving up. (Both number of retries and the pause duration are configurable).     

# Future considerations

### 1. Merge `StructuredLogger` and `EventDestinationCollection` in some way that will remove the additional method call overhead.

Provide benchmarks with proof that this improves performance.
 
A possible problem with this may come from the fact that if we want to keep the atomicity we may need to use a data structure that holds the writer objects and that level of indirection may slow things down to a point where it's slower that the cost of the method call.  

### 2. Preserve destination buffer when reconfiguring

Provide some way of reconfiguring the event destination objects that preserves their buffers.

Think of a destination implementation that sends events to ELK. The application was started and some events were generated but the ELK destination was configured incorrectly - the ELK credentials were wrong.

After a manual configuration step correct credentials are provided but during the reconfiguration process all events in the buffer are lost.

Currently when an error occurs the retry mechanism tries to flush the events multiple times before giving up, maybe change the condition for giving up from number of failed flush attempts to a number items in the buffer. That way we allow more time for manual intervention and also improve the case, where some API is down for more time than the current retry mechanism allows, but in the meantime event frequency is low and we would be throwing away events that we can afford to keep in memory for a little while longer.

### 3. Find a better way to know when an `EventDestinationCollection` instance is no longer in use

Currently, when `EventDestinationCollection.Log` is called the field `ConcurrentLogCalls` is incremented and then decremented on exit. This is done so that after a `EventDestinationCollection` instance is no longer used we can tell if there are any ongoing calls to `Log`. This is done to ensure that no new events will be enqueued while the event destination instances are disposed. The problem is that in order to increment and decrement atomically, `Interlocked.Increment` and `Interlocked.Decrement` are used, which are expensive and are executed synchronously on the thread that calls `Log`.

Maybe find a better way of ensuring that `EventDestinationCollection.DisposeAsync` waits for all calls to `Log` to finish.
