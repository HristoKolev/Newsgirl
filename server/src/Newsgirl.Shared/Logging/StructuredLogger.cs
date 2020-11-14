namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    /// <summary>
    /// The primary logging API. This is what gets passed around by IoC and used by other code.
    /// </summary>
    public interface Log
    {
        /// <summary>
        /// Enqueues an event providing the event stream name and a function that creates the event.
        /// If the event stream is not enabled or no destinations exists for that event stream,
        /// the function will not be called.
        /// </summary>
        void Log<TEventData>(string eventStreamName, Func<TEventData> item);
    }

    /// <summary>
    /// This is the API that we use to define all possible event streams and their destinations.
    /// </summary>
    public class StructuredLoggerBuilder
    {
        /// <summary>
        /// This is the resulting data structure. The key is the event stream name and the value is a function that
        /// takes the current event source configuration and returns an array of event destinations for that event source.
        /// </summary>
        private readonly Dictionary<string, Func<EventStreamConfig[], object[]>> destinationsFactoryMap
            = new Dictionary<string, Func<EventStreamConfig[], object[]>>();

        public void AddEventStream<TEventData>(string eventStreamName, Dictionary<string, Func<EventDestination<TEventData>>> factoryMap)
        {
            // TODO: check for null

            if (this.destinationsFactoryMap.ContainsKey(eventStreamName))
            {
                throw new DetailedException("There already is an event stream with this name.")
                {
                    Details =
                    {
                        {"eventStreamName", eventStreamName},
                    },
                };
            }

            this.destinationsFactoryMap.Add(eventStreamName, eventStreamConfigArray =>
            {
                var eventStreamConfig = eventStreamConfigArray.FirstOrDefault(x => x.Name == eventStreamName);

                if (eventStreamConfig == null || !eventStreamConfig.Enabled)
                {
                    return null;
                }

                var destinations = new List<EventDestination<TEventData>>();

                foreach (var (destinationName, destinationFactory) in factoryMap)
                {
                    var destinationConfig = eventStreamConfig.Destinations.FirstOrDefault(x => x.Name == destinationName);

                    if (destinationConfig == null || !destinationConfig.Enabled)
                    {
                        continue;
                    }

                    var destination = destinationFactory();

                    destination.Start();

                    destinations.Add(destination);
                }

                if (!destinations.Any())
                {
                    return null;
                }

                // ReSharper disable once CoVariantArrayConversion
                return destinations.ToArray();
            });
        }

        /// <summary>
        /// Creates a StructuredLogger instance with the current configuration.
        /// </summary>
        public StructuredLogger Build()
        {
            // TODO: lock this instance after this method call.
            return new StructuredLogger(this.destinationsFactoryMap);
        }
    }

    public class EventStreamConfig
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }

        public EventDestinationConfig[] Destinations { get; set; }
    }

    public class EventDestinationConfig
    {
        public string Name { get; set; }

        public bool Enabled { get; set; }
    }

    /// <summary>
    /// An implementation of Log that can hot swap EventDestinationCollection instances when the configuration changes.
    /// Dispatches all events to the current EventDestinationCollection instance.
    /// Reconfigure must be called before any calls to Log.
    /// </summary>
    public class StructuredLogger : Log, IAsyncDisposable
    {
        /// <summary>
        /// The key is the event stream name and the value is a function that takes the current event source
        /// configuration and returns an array of event destinations for that event source.
        /// </summary>
        private readonly Dictionary<string, Func<EventStreamConfig[], object[]>> destinationsFactoryMap;

        /// <summary>
        /// Events received by calls of Log are delegated to this instance.
        /// </summary>
        private EventDestinationCollection eventDestinationCollection;

        public StructuredLogger(Dictionary<string, Func<EventStreamConfig[], object[]>> destinationsFactoryMap)
        {
            this.destinationsFactoryMap = destinationsFactoryMap;
        }

        /// <summary>
        /// Takes an event stream configuration array and builds a new destination collection replacing the old one.
        /// The old collection gets disposed.
        /// Must be called before any calls to Log.
        /// </summary>
        public async ValueTask Reconfigure(EventStreamConfig[] eventStreamConfigArray)
        {
            var destinationsByEventStreamName = new Dictionary<string, object[]>();

            foreach (var (eventStreamName, destinationFactory) in this.destinationsFactoryMap)
            {
                var destinations = destinationFactory(eventStreamConfigArray);

                if (destinations == null)
                {
                    continue;
                }

                destinationsByEventStreamName.Add(eventStreamName, destinations);
            }

            var oldCollection = this.eventDestinationCollection;

            this.eventDestinationCollection = EventDestinationCollection.Build(destinationsByEventStreamName);

            if (oldCollection != null)
            {
                await oldCollection.DisposeAsync();
            }
        }

        public void Log<TEventData>(string eventStreamName, Func<TEventData> item)
        {
            this.eventDestinationCollection.Log(eventStreamName, item);
        }

        public ValueTask DisposeAsync()
        {
            // TODO: test dispose before any call to Reconfigure.
            return this.eventDestinationCollection?.DisposeAsync() ?? new ValueTask();
        }
    }

    /// <summary>
    /// An implementation of Log that provides a performant way of dispatching events to their destinations
    /// based on their event stream names.
    /// </summary>
    public abstract class EventDestinationCollection : Log, IAsyncDisposable
    {
        private Dictionary<string, object[]> destinationsByEventStreamName;

        /// <summary>
        /// This counts how many concurrent calls to Log are executing at a specific moment.
        /// Gets incremented at the start of the Log call and gets decremented at the end.
        /// </summary>
        protected int ConcurrentLogCalls = 0;

        /// <summary>
        /// Gets implemented by the generated class.
        /// No blocking work is done here. Destinations receive the events asynchronously.
        /// </summary>
        public abstract void Log<TEventData>(string eventStreamName, Func<TEventData> item);

        /// <summary>
        /// Waits until all calls to Log have exited and then disposes all event destinations concurrently.
        /// </summary>
        public async ValueTask DisposeAsync()
        {
            while (this.ConcurrentLogCalls != 0)
            {
                await Task.Delay(10);
            }

            var disposeTasks = new List<Task>();

            foreach (var destinationsArray in this.destinationsByEventStreamName.Values)
            {
                for (int i = 0; i < destinationsArray.Length; i++)
                {
                    var destination = (EventDestination) destinationsArray[i];

                    disposeTasks.Add(destination.Stop());
                }
            }

            await Task.WhenAll(disposeTasks);
        }

        /// <summary>
        /// Takes a map of event stream name and array of destinations and creates an instance
        /// of EventDestinationCollection that is optimized for that specific configuration.
        /// </summary>
        public static EventDestinationCollection Build(Dictionary<string, object[]> destinationsByEventStreamName)
        {
            var typeBuilder = ReflectionEmmitHelper.ModuleBuilder.DefineType(
                nameof(EventDestinationCollection) + "+" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(EventDestinationCollection)
            );

            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ret);

            EmitLog(typeBuilder, destinationsByEventStreamName);

            var type = typeBuilder.CreateType();

            var instance = (EventDestinationCollection) Activator.CreateInstance(type!);

            foreach (var (eventStreamName, destinationArray) in destinationsByEventStreamName)
            {
                var writers = destinationArray.Cast<EventDestination>().Select(x => x.GetWriter()).ToArray();

                var field = type.GetField(eventStreamName + "_writers", BindingFlags.Public | BindingFlags.Instance);

                field!.SetValue(instance, writers);
            }

            instance!.destinationsByEventStreamName = destinationsByEventStreamName;

            return instance;
        }

        private static void EmitLog(TypeBuilder typeBuilder, Dictionary<string, object[]> destinationsByEventStreamName)
        {
            // Interlocked.Increment
            var incrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == nameof(Interlocked.Increment) && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());

            // Interlocked.Decrement
            var decrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == nameof(Interlocked.Decrement) && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());

            // EventDestinationCollection.ConcurrentLogCalls
            var callCounterField = typeof(EventDestinationCollection).GetField(nameof(ConcurrentLogCalls), BindingFlags.NonPublic | BindingFlags.Instance)!;

            var logMethod = typeBuilder.DefineMethod(nameof(Log),
                MethodAttributes.Public |
                MethodAttributes.ReuseSlot |
                MethodAttributes.HideBySig |
                MethodAttributes.Final |
                MethodAttributes.Virtual
            );

            var typeParameter = logMethod.DefineGenericParameters("TLogData")[0];

            logMethod.SetParameters(typeof(string), typeof(Func<>).MakeGenericType(typeParameter));
            logMethod.SetReturnType(typeof(void));

            var il = logMethod.GetILGenerator();
            var itemLocal = il.DeclareLocal(typeParameter);
            var arrayIndexLocal = il.DeclareLocal(typeof(int));
            var writerArrayLocal = il.DeclareLocal(typeof(object[]));

            var exitLabel = il.DefineLabel();
            var afterWritersSelect = il.DefineLabel();

            foreach (string eventStreamName in destinationsByEventStreamName.Keys)
            {
                var field = typeBuilder.DefineField(eventStreamName + "_writers", typeof(object[]), FieldAttributes.Public);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, eventStreamName);
                il.Emit(OpCodes.Call, typeof(string).GetMethod("op_Equality")!);

                var nextLabel = il.DefineLabel();
                il.Emit(OpCodes.Brfalse_S, nextLabel);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Stloc, writerArrayLocal);

                il.Emit(OpCodes.Br_S, afterWritersSelect);

                il.MarkLabel(nextLabel);
            }

            il.Emit(OpCodes.Br_S, exitLabel);

            il.MarkLabel(afterWritersSelect);

            // Interlocked.Increment(ref this.ConcurrentLogCalls);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, callCounterField!);
            il.Emit(OpCodes.Call, incrementMethod);
            il.Emit(OpCodes.Pop);

            il.BeginExceptionBlock();

            // itemLocal = arg2.Invoke()
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                typeof(Func<>).MakeGenericType(typeParameter),
                typeof(Func<>).GetMethod(nameof(Func<object>.Invoke))!)
            );
            il.Emit(OpCodes.Stloc, itemLocal);

            var loopCheckLabel = il.DefineLabel();
            var loopBodyLabel = il.DefineLabel();

            // Declare index local
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);
            il.Emit(OpCodes.Br_S, loopCheckLabel);

            // Body step.
            il.MarkLabel(loopBodyLabel);

            // Load the writer. writerArrayLocal[i]
            il.Emit(OpCodes.Ldloc, writerArrayLocal);
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldelem_Ref);

            // Call TryWrite.
            il.Emit(OpCodes.Ldloc, itemLocal);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                typeof(ChannelWriter<>).MakeGenericType(typeParameter),
                typeof(ChannelWriter<>).GetMethod(nameof(ChannelWriter<object>.TryWrite))!
            ));
            il.Emit(OpCodes.Pop);

            // TODO: throw if false 

            // Increment step. i++
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);

            // Compare step.
            il.MarkLabel(loopCheckLabel);

            // Load the index local.
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);

            // writerArrayLocal.Length
            il.Emit(OpCodes.Ldloc, writerArrayLocal);
            il.Emit(OpCodes.Ldlen);

            // Compare.
            il.Emit(OpCodes.Blt_S, loopBodyLabel);

            il.BeginFinallyBlock();

            // Interlocked.Decrement(ref this.ConcurrentLogCalls);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, callCounterField!);
            il.Emit(OpCodes.Call, decrementMethod);
            il.Emit(OpCodes.Pop);

            il.EndExceptionBlock();

            il.MarkLabel(exitLabel);

            il.Emit(OpCodes.Ret);
        }
    }

    /// <summary>
    /// Base class for all event destinations.
    /// When started enqueues a task on the thread pool that reads items from it's Channel{TEventData} instance.
    /// Buffers events in an internal buffer and calls the abstract Flush passing an ArraySegment{TEventData}
    /// to the implementer.
    /// If the implementer throws an exception Flush is retried for configurable number of times with a configurable
    /// pause between each call.
    /// </summary>
    public abstract class EventDestination<TEventData> : EventDestination
    {
        private readonly ErrorReporter errorReporter;
        private Task readTask;
        private TEventData[] buffer;
        private bool started;
        private Channel<TEventData> channel;

        protected EventDestination(ErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
        }

        protected TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(30);

        protected int NumberOfRetries { get; set; } = 10;

        protected TimeSpan TimeBetweenMainLoopRestart { get; set; } = TimeSpan.FromSeconds(1);

        protected abstract ValueTask Flush(ArraySegment<TEventData> data);

        public void Start()
        {
            if (this.started)
            {
                throw new DetailedException("The destination is already started.");
            }

            this.buffer = ArrayPool<TEventData>.Shared.Rent(16);
            this.channel = Channel.CreateUnbounded<TEventData>();
            this.readTask = Task.Run(this.Read);

            this.started = true;
        }

        public async Task Stop()
        {
            if (!this.started)
            {
                throw new DetailedException("The destination is already stopped.");
            }

            try
            {
                this.channel.Writer.Complete();
                await this.readTask;
            }
            finally
            {
                ArrayPool<TEventData>.Shared.Return(this.buffer);
            }

            this.channel = null;
            this.readTask = null;
            this.buffer = null;

            this.started = false;
        }

        public object GetWriter()
        {
            return this.channel.Writer;
        }

        private async Task Read()
        {
            while (true)
            {
                try
                {
                    while (await this.channel.Reader.WaitToReadAsync())
                    {
                        TEventData item;
                        int i = 0;

                        for (; this.channel.Reader.TryRead(out item); i += 1)
                        {
                            if (i == this.buffer.Length)
                            {
                                this.ResizeBuffer();
                            }

                            this.buffer[i] = item;
                        }

                        var segment = new ArraySegment<TEventData>(this.buffer, 0, i);

                        for (int j = 0; j < this.NumberOfRetries + 1; j++)
                        {
                            try
                            {
                                await this.Flush(segment);
                                break;
                            }
                            catch (Exception exception)
                            {
                                await this.errorReporter.Error(exception, "STRUCTURED_LOGGER_FAILED_TO_FLUSH", new Dictionary<string, object>
                                {
                                    {"EventDestinationType", this.GetType().FullName},
                                    {"EventDataType", typeof(TEventData).FullName},
                                });

                                await Task.Delay(this.TimeBetweenRetries);
                            }
                        }
                    }

                    break;
                }
                // This should never happen if this code functions normally.
                // Exceptions thrown in custom Flush implementations are caught earlier.
                // The only way we get here is if the implementation of this method throws an exception.
                catch (Exception exception)
                {
                    await this.errorReporter.Error(exception, "STRUCTURED_LOGGER_CRITICAL_ERROR", new Dictionary<string, object>
                    {
                        {"EventDestinationType", this.GetType().FullName},
                        {"EventDataType", typeof(TEventData).FullName},
                    });

                    await Task.Delay(this.TimeBetweenMainLoopRestart);
                }
            }
        }

        private void ResizeBuffer()
        {
            TEventData[] newBuffer = null;

            try
            {
                newBuffer = ArrayPool<TEventData>.Shared.Rent(this.buffer.Length * 2);

                Array.Copy(this.buffer, newBuffer, this.buffer.Length);
            }
            catch (Exception)
            {
                if (newBuffer != null)
                {
                    ArrayPool<TEventData>.Shared.Return(newBuffer);
                }

                throw;
            }

            ArrayPool<TEventData>.Shared.Return(this.buffer);

            this.buffer = newBuffer;
        }
    }

    /// <summary>
    /// This interface exists to allow some operations on EventDestination{T} that do not need T to be exposed.
    /// </summary>
    public interface EventDestination
    {
        Task Stop();

        object GetWriter();
    }
}
