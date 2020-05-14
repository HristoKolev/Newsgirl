namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Runtime.CompilerServices;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    
    /// <summary>
    /// Logging service for structured data.
    /// </summary>
    public class StructuredLogger : IAsyncDisposable, ILog
    {
        private readonly Action<StructuredLoggerBuilder> buildLogger;
        private LogConsumerCollection consumerCollection = new LogConsumerCollection();

        public StructuredLogger(Action<StructuredLoggerBuilder> buildLogger)
        {
            this.buildLogger = buildLogger ?? throw new DetailedLogException("The build function is null.");
        }

        public void Log<T>(string key, Func<T> func)
        {
            var collection = this.consumerCollection;
            
            collection.IncrementRc();

            try
            {
                var consumers = collection.GetConsumers<T>(key);

                if (consumers == null)
                {
                    return;
                }

                var item = func();

                for (int i = 0; i < consumers.Length; i++)
                {
                    if (!consumers[i].Channel.Writer.TryWrite(item))
                    {
                        throw new ApplicationException("channel.Writer.TryWrite returned false.");
                    }
                }
            }
            finally
            {
                collection.DecrementRc();
            }
        }

        public async ValueTask Reconfigure(StructuredLoggerConfig[] configArray)
        {
            var builder = new StructuredLoggerBuilder();
            this.buildLogger(builder);
            
            var map = new Dictionary<string, object>();

            foreach (var (configName, consumersFactory) in builder.LogConsumersFactoryMap)
            {
                var consumers = consumersFactory(configArray);
                
                if (consumers == null)
                {
                    continue;
                }
                
                map.Add(configName, consumers);
            }
            
            var oldCollection = this.consumerCollection;
            
            this.consumerCollection = new LogConsumerCollection(map);
            
            await oldCollection.WaitUntilUnused();
            await oldCollection.DisposeAsync();
        }

        public async ValueTask DisposeAsync()
        {
            await this.consumerCollection.WaitUntilUnused();
            await this.consumerCollection.DisposeAsync();
        }

        private class LogConsumerCollection : IAsyncDisposable
        {
            private readonly Dictionary<string, object> consumersByConfigName;

            private int referenceCount;

            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void IncrementRc()
            {
                Interlocked.Increment(ref this.referenceCount);
            }
        
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            public void DecrementRc()
            {
                Interlocked.Decrement(ref this.referenceCount);
            }

            public Task WaitUntilUnused()
            {
                return Task.Run(async () =>
                {
                    while (this.referenceCount != 0)
                    {
                        await Task.Delay(100);
                    }
                });
            }

            public LogConsumerCollection(Dictionary<string,object> consumersByConfigName)
            {
                this.consumersByConfigName = consumersByConfigName;
            }

            public LogConsumerCollection()
            {
                this.consumersByConfigName = new Dictionary<string, object>();
            }

            public LogConsumer<T>[] GetConsumers<T>(string key)
            {
                if (!this.consumersByConfigName.TryGetValue(key, out var consumersObj))
                {
                    return null;
                }

                return (LogConsumer<T>[])consumersObj;
            }
        
            public async ValueTask DisposeAsync()
            {
                var disposeTasks = new List<Task>();

                foreach (var consumersObj in this.consumersByConfigName.Values)
                {
                    foreach (var consumer in ((IEnumerable)consumersObj).Cast<LogConsumerLifetime>())
                    {
                        disposeTasks.Add(consumer.Stop());
                    }
                }

                await Task.WhenAll(disposeTasks);
            }
        }
    }

    /// <summary>
    /// Builder for the <see cref="StructuredLogger"/>.
    /// TODO: define invalid config behaviour. 
    /// </summary>
    public class StructuredLoggerBuilder
    {
        public Dictionary<string, Func<StructuredLoggerConfig[], object>> LogConsumersFactoryMap { get; }
            = new Dictionary<string, Func<StructuredLoggerConfig[], object>>(); 
        
        public void AddConfig<T>(string configName, Dictionary<string, Func<LogConsumer<T>>> consumerFactoryMap)
        {
            if (this.LogConsumersFactoryMap.ContainsKey(configName))
            {
                throw new DetailedLogException("There already is a configuration with this name.")
                {
                    Details =
                    {
                        {"configName", configName},
                    }
                };
            }

            this.LogConsumersFactoryMap.Add(configName, configArray =>
            {
                var config = configArray.FirstOrDefault(x => x.Name == configName);

                if (config == null || !config.Enabled)
                {
                    return null;
                }
                
                var consumers = new List<LogConsumer<T>>();

                foreach (var (consumerName, consumerFactory) in consumerFactoryMap)
                {
                    var consumerConfig = config.Consumers.FirstOrDefault(x => x.Name == consumerName);

                    if (consumerConfig == null || !consumerConfig.Enabled)
                    {
                        continue;
                    }

                    var consumer = consumerFactory();
                    
                    consumer.Start();
                    
                    consumers.Add(consumer);
                }

                if (!consumers.Any())
                {
                    return null;
                }
                
                return consumers.ToArray();
            });
        }
    }
 
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StructuredLoggerConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }

        public StructuredLoggerConsumerConfig[] Consumers { get; set; }
    }
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class StructuredLoggerConsumerConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }
    }
    
    public interface ILog
    {
        void Log<T>(string key, Func<T> func);
    }

    public interface LogConsumerLifetime
    {
        void Start();
        
        Task Stop();
    }

    /// <summary>
    /// Base class for all log consumers.
    /// </summary>
    /// <typeparam name="TData">The type of data that the consumer consumes.</typeparam>
    public abstract class LogConsumer<TData> : LogConsumerLifetime
    {
        private readonly ErrorReporter errorReporter;
        private Task runningTask;
        private TData[] buffer;
        private bool started;

        protected LogConsumer(ErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
        }
        
        protected TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(5);

        protected int NumberOfRetries { get; set; } = 5;

        protected TimeSpan TimeBetweenMainLoopRestart { get; set; } = TimeSpan.FromSeconds(1);

        public Channel<TData> Channel { get; private set; }
        
        public void Start()
        {
            if (this.started)
            {
                throw new DetailedLogException("The consumer is already started.");
            }
            
            this.buffer = ArrayPool<TData>.Shared.Rent(16);
            this.Channel = System.Threading.Channels.Channel.CreateUnbounded<TData>();
            this.runningTask = Task.Run(this.ReadFromChannel);

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
                await this.runningTask;
            }
            finally
            {
                ArrayPool<TData>.Shared.Return(this.buffer);    
            }

            this.started = false;
        }

        private async Task ReadFromChannel()
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

                        Exception exception = null;
                    
                        for (int j = 0; j < this.NumberOfRetries + 1; j++)
                        {
                            try
                            {
                                await this.Flush(segment);
                                
                                exception = null;
                                break;
                            }
                            catch (Exception ex)
                            {
                                exception = ex;
                                
                                await Task.Delay(this.TimeBetweenRetries);
                            }
                        }

                        if (exception != null)
                        {
                            await this.errorReporter.Error(exception);
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
}
