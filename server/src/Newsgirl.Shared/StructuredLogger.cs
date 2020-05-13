namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Diagnostics;
    using System.Linq;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    
    /// <summary>
    /// Logging service for structured data.
    /// TODO: Solve the apparent race condition. 
    /// </summary>
    public class StructuredLogger : IAsyncDisposable, ILog
    {
        private readonly Action<StructuredLoggerBuilder> buildLogger;
        private LogProducerCollection producerCollection = new LogProducerCollection();

        public StructuredLogger(Action<StructuredLoggerBuilder> buildLogger)
        {
            this.buildLogger = buildLogger ?? throw new DetailedLogException("The build function is null.");
        }

        public void Log<T>(string key, Func<T> func)
        {
            var producer = this.producerCollection.GetProducer<T>(key);

            if (producer == null)
            {
                return;
            }

            var item = func();

            producer.Enqueue(item);
        }

        public ValueTask Reconfigure(StructuredLoggerConfig[] configArray)
        {
            var builder = new StructuredLoggerBuilder();
            this.buildLogger(builder);
            
            var map = new Dictionary<string, object>();

            foreach (var (configName, producerFactory) in builder.LogProducerFactoryMap)
            {
                var producer = producerFactory(configArray);
                
                if (producer == null)
                {
                    continue;
                }
                
                map.Add(configName, producer);
            }
            
            var oldCollection = this.producerCollection;
            this.producerCollection = new LogProducerCollection(map);
            return oldCollection.DisposeAsync();
        }

        public ValueTask DisposeAsync()
        {
            return this.producerCollection.DisposeAsync();
        }
    }

    /// <summary>
    /// Builder for the <see cref="StructuredLogger"/>.
    /// TODO: define invalid config behaviour. 
    /// </summary>
    public class StructuredLoggerBuilder
    {
        public Dictionary<string, Func<StructuredLoggerConfig[], object>> LogProducerFactoryMap { get; }
            = new Dictionary<string, Func<StructuredLoggerConfig[], object>>(); 
        
        public void AddConfig<T>(string configName, Dictionary<string, Func<LogConsumer<T>>> consumerFactoryMap)
        {
            if (this.LogProducerFactoryMap.ContainsKey(configName))
            {
                throw new DetailedLogException("There already is a configuration with this name.")
                {
                    Details =
                    {
                        {"configName", configName},
                    }
                };
            }

            this.LogProducerFactoryMap.Add(configName, configArray =>
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
                    
                    consumers.Add(consumerFactory());
                }

                if (!consumers.Any())
                {
                    return null;
                }
                
                return new LogProducer<T>(consumers.ToArray());
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
    
    public class LogProducerCollection : IAsyncDisposable
    {
        private readonly Dictionary<string, object> producersByConfigName;

        public LogProducerCollection(Dictionary<string,object> producersByConfigName)
        {
            this.producersByConfigName = producersByConfigName;
        }

        public LogProducerCollection()
        {
            this.producersByConfigName = new Dictionary<string, object>();
        }

        public LogProducer<T> GetProducer<T>(string key)
        {
            if (!this.producersByConfigName.TryGetValue(key, out var producerObj))
            {
                return null;
            }

            var producer = (LogProducer<T>)producerObj;

            return producer;
        }
        
        public async ValueTask DisposeAsync()
        {
            var disposeTasks = new List<Task>();

            foreach (var producer in this.producersByConfigName.Values)
            {
                if (producer is IAsyncDisposable x)    
                {
                    disposeTasks.Add(x.DisposeAsync().AsTask());
                }
            }

            await Task.WhenAll(disposeTasks);
        }
    }
    
    public class LogProducer<T> : IAsyncDisposable
    {
        private readonly LogConsumer<T>[] consumers;
        
        public LogProducer(LogConsumer<T>[] consumers)
        {
            this.consumers = consumers;

            foreach (var consumer in consumers)
            {
                consumer.Start();
            }
        }
 
        public void Enqueue(T item)
        {
            for (int i = 0; i < this.consumers.Length; i++)
            {
                if (!this.consumers[i].Channel.Writer.TryWrite(item))
                {
                    throw new ApplicationException("channel.Writer.TryWrite returned false.");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            var disposeTasks = new List<Task>();

            foreach (var consumer in this.consumers)
            {
                disposeTasks.Add(consumer.DisposeAsync().AsTask());
            }

            await Task.WhenAll(disposeTasks);
        }
    }
    
    /// <summary>
    /// Base class for all log consumers.
    /// </summary>
    /// <typeparam name="TData">The type of data that the consumer consumes.</typeparam>
    public abstract class LogConsumer<TData> : IAsyncDisposable
    {
        private readonly ErrorReporter errorReporter;
        private Task runningTask;
        private TData[] buffer;
        
        protected LogConsumer(ErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
            this.buffer = ArrayPool<TData>.Shared.Rent(16);
            this.Channel = System.Threading.Channels.Channel.CreateUnbounded<TData>();
        }
        
        protected TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(5);

        protected int NumberOfRetries { get; set; } = 5;

        protected TimeSpan TimeBetweenMainLoopRestart { get; set; } = TimeSpan.FromSeconds(1);

        public Channel<TData> Channel { get; }
        
        public void Start()
        {
            this.runningTask = Task.Run(this.ReadFromChannel);
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

        public async ValueTask DisposeAsync()
        {
            try
            {
                Trace.WriteLine("complete " + this.GetType().Name);
                
                this.Channel.Writer.Complete();
                
                await this.runningTask;
            }
            finally
            {
                ArrayPool<TData>.Shared.Return(this.buffer);    
            }
        }
    }
}
