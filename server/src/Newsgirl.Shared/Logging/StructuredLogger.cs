namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class StructuredLoggerBuilder
    {
        private readonly Dictionary<string, Func<EventStreamConfig[], object>> destinationsFactoryMap
            = new Dictionary<string, Func<EventStreamConfig[], object>>(); 
        
        public void AddEventStream<T>(string eventStreamName, Dictionary<string, Func<LogDestination<T>>> factoryMap)
        {
            if (this.destinationsFactoryMap.ContainsKey(eventStreamName))
            {
                throw new DetailedLogException("There already is an event stream with this name.")
                {
                    Details =
                    {
                        {"eventStreamName", eventStreamName},
                    }
                };
            }

            this.destinationsFactoryMap.Add(eventStreamName, eventStreamConfigArray =>
            {
                var eventStreamConfig = eventStreamConfigArray.FirstOrDefault(x => x.Name == eventStreamName);

                if (eventStreamConfig == null || !eventStreamConfig.Enabled)
                {
                    return null;
                }
                
                var destinations = new List<LogDestination<T>>();

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
                
                return destinations.ToArray();
            });
        }

        public StructuredLogger Build()
        {
            return new StructuredLogger(this.destinationsFactoryMap);
        }
    }
    
    public class EventStreamConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }

        public LogDestinationConfig[] Destinations { get; set; }
    }
    
    public class LogDestinationConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }
    }
    
    public class StructuredLogger : IAsyncDisposable, ILog
    {
        private readonly Dictionary<string, Func<EventStreamConfig[], object>> factoryMap;

        private LogConsumerCollection destinationCollection;

        public StructuredLogger(Dictionary<string, Func<EventStreamConfig[], object>> factoryMap)
        {
            this.factoryMap = factoryMap;
        }

        public async ValueTask Reconfigure(EventStreamConfig[] configArray)
        {
            var map = new Dictionary<string, object>();

            foreach (var (configName, destinationFactory) in this.factoryMap)
            {
                var destinations = destinationFactory(configArray);
                
                if (destinations == null)
                {
                    continue;
                }
                
                map.Add(configName, destinations);
            }
            
            var oldCollection = this.destinationCollection;
            
            this.destinationCollection = LogConsumerCollection.Build(map);

            if (oldCollection != null)
            {
                await oldCollection.DisposeAsync();    
            }
        }

        public void Log<TData>(string configName, Func<TData> item)
        {
            this.destinationCollection.Log(configName, item);
        }

        public ValueTask DisposeAsync()
        {
            return this.destinationCollection.DisposeAsync();
        }
    }

    public interface ILog
    {
        void Log<TData>(string configName, Func<TData> item); 
    }
    
    public abstract class LogConsumerCollection
    {
        private Dictionary<string, object> consumersByConfigName;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once ConvertToConstant.Global
        protected int ReferenceCount = 0;
        
        public async ValueTask DisposeAsync()
        {
            while (this.ReferenceCount != 0)
            {
                await Task.Delay(10);
            }
            
            var disposeTasks = new List<Task>();

            foreach (var consumersObj in this.consumersByConfigName.Values)
            {
                foreach (var consumer in ((IEnumerable)consumersObj).Cast<LogConsumerControl>())
                {
                    disposeTasks.Add(consumer.Stop());
                }
            }

            await Task.WhenAll(disposeTasks);
        }

        public abstract void Log<TData>(string configName, Func<TData> item);
        
        public static LogConsumerCollection Build(Dictionary<string, object> map)
        {
            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType(
                nameof(LogConsumerCollection) + "+" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(LogConsumerCollection)
            );

            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ret);
            
            EmitLog(typeBuilder, map);

            var type = typeBuilder.CreateType();

            var instance = (LogConsumerCollection) Activator.CreateInstance(type);
            
            foreach (var (configName, consumersObj) in map)
            {
                var array = ((IEnumerable) consumersObj)
                    .Cast<LogConsumerControl>()
                    .Select(consumer => consumer.GetWriter())
                    .ToArray();

                var field = type.GetField(configName + "_writers", BindingFlags.Public | BindingFlags.Instance);
                
                field!.SetValue(instance, array);
            }

            instance!.consumersByConfigName = map;

            return instance;
        }

        private static void EmitLog(TypeBuilder typeBuilder, Dictionary<string, object> map)
        {
            // Interlocked.Increment
            var incrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == "Increment" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());
            
            // Interlocked.Decrement
            var decrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == "Decrement" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());
            
            // LogConsumerCollection.ReferenceCount
            var referenceCountField = typeof(LogConsumerCollection).GetField("ReferenceCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
            
            var logMethod = typeBuilder.DefineMethod(
                "Log",
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
            
            foreach (string configName in map.Keys)
            {
                var field = typeBuilder.DefineField(configName + "_writers", typeof(object[]), FieldAttributes.Public);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, configName);
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
            
            // Interlocked.Increment(ref this.ReferenceCount);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, referenceCountField!);
            il.Emit(OpCodes.Call, incrementMethod);
            il.Emit(OpCodes.Pop);

            il.BeginExceptionBlock();

            // itemLocal = arg2.Invoke()
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                typeof(Func<>).MakeGenericType(typeParameter),
                typeof(Func<>).GetMethod("Invoke")!)
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
                typeof(ChannelWriter<>).GetMethod("TryWrite")!
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
            
            // Interlocked.Decrement(ref this.ReferenceCount);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, referenceCountField!);
            il.Emit(OpCodes.Call, decrementMethod);
            il.Emit(OpCodes.Pop);

            il.EndExceptionBlock();

            il.MarkLabel(exitLabel);

            il.Emit(OpCodes.Ret);
        }
    }
        
    /// <summary>
    /// Base class for all log consumers.
    /// </summary>
    public abstract class LogDestination<TData> : LogConsumerControl
    {
        private readonly ErrorReporter errorReporter;
        private Task readTask;
        private TData[] buffer;
        private bool started;

        protected LogDestination(ErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
        }
        
        protected TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(30);

        protected int NumberOfRetries { get; set; } = 10;

        protected TimeSpan TimeBetweenMainLoopRestart { get; set; } = TimeSpan.FromSeconds(1);

        private Channel<TData> Channel { get; set; }

        public void Start()
        {
            if (this.started)
            {
                throw new DetailedLogException("The consumer is already started.");
            }
            
            this.buffer = ArrayPool<TData>.Shared.Rent(16);
            this.Channel = System.Threading.Channels.Channel.CreateUnbounded<TData>();
            this.readTask = Task.Run(this.Read);

            this.started = true;
        }
        
        public async Task Stop()
        {
            if (!this.started)
            {
                throw new DetailedLogException("The consumer is already stopped.");
            }
            
            try
            {
                this.Channel.Writer.Complete();
                await this.readTask;
            }
            finally
            {
                ArrayPool<TData>.Shared.Return(this.buffer);    
            }

            this.Channel = null;
            this.readTask = null;
            this.buffer = null;

            this.started = false;
        }

        public object GetWriter()
        {
            return this.Channel.Writer;
        }

        private async Task Read()
        {
            while (true)
            {
                try
                {
                    while (await this.Channel.Reader.WaitToReadAsync())
                    {
                        TData item;
                        int i = 0;
                    
                        for (; this.Channel.Reader.TryRead(out item); i += 1)
                        {
                            if (i == this.buffer.Length)
                            {
                                this.ResizeBuffer();
                            }

                            this.buffer[i] = item;
                        }

                        var segment = new ArraySegment<TData>(this.buffer, 0, i);

                        for (int j = 0; j < this.NumberOfRetries + 1; j++)
                        {
                            try
                            {
                                await this.Flush(segment);
                                break;
                            }
                            catch (Exception exception)
                            {
                                await this.errorReporter.Error(exception);
                                await Task.Delay(this.TimeBetweenRetries);
                            }
                        }
                    }

                    break;
                }
                // This should never happen if this code functions normally.
                // Exceptions thrown in custom Flush implementations are caught earlier.
                // The only way we get here is if the implementation of this method throws an exception.
                catch (Exception ex)  
                {
                    await this.errorReporter.Error(ex);
                    
                    await Task.Delay(this.TimeBetweenMainLoopRestart);
                }
            }
        }

        private void ResizeBuffer()
        {
            TData[] newBuffer = null;

            try
            {
                newBuffer = ArrayPool<TData>.Shared.Rent(this.buffer.Length * 2);
                
                Array.Copy(this.buffer, newBuffer, this.buffer.Length);
            }
            catch (Exception)
            {
                if (newBuffer != null)
                {
                    ArrayPool<TData>.Shared.Return(newBuffer);                                        
                }
                                    
                throw;
            }

            ArrayPool<TData>.Shared.Return(this.buffer);
            
            this.buffer = newBuffer;
        }

        protected abstract ValueTask Flush(ArraySegment<TData> data);
    }

    public interface LogConsumerControl
    {
        Task Stop();

        object GetWriter();
    }
}
