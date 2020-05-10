namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class LogProducer<T> : IAsyncDisposable
    {
        private readonly LogConsumer<T>[] consumers;
        private readonly Channel<T>[] channels;
        private readonly UnboundedChannelOptions channelOptions = new UnboundedChannelOptions
        {
            SingleReader = true,
        };
 
        public LogProducer(LogConsumer<T>[] consumers)
        {
            this.consumers = consumers;
            this.channels = new Channel<T>[consumers.Length];
            
            for (int i = 0; i < consumers.Length; i++)
            {
                var channel = Channel.CreateUnbounded<T>(this.channelOptions);
                this.channels[i] = channel;
                consumers[i].Start(channel.Reader);
            }
        }
 
        public void Enqueue(T item)
        {
            for (int i = 0; i < this.channels.Length; i++)
            {
                var channel = this.channels[i];

                if (!channel.Writer.TryWrite(item))
                {
                    throw new ApplicationException("channel.Writer.TryWrite returned false.");
                }
            }
        }

        public async ValueTask DisposeAsync()
        {
            var exitTasks = new Task[this.consumers.Length];
            
            for (int i = 0; i < this.consumers.Length; i++)
            {
                this.channels[i].Writer.Complete();
                exitTasks[i] = this.consumers[i].Stop();
            }

            await Task.WhenAll(exitTasks);
        }
    }
    
    public interface LogConsumer<T>
    {
        void Start(ChannelReader<T> channelReader);

        Task Stop();
    }
    
    public abstract class LogConsumerBase<T> : LogConsumer<T>
    {
        // ReSharper disable once MemberCanBePrivate.Global
        protected readonly ErrorReporter ErrorReporter;

        protected LogConsumerBase(ErrorReporter errorReporter)
        {
            this.ErrorReporter = errorReporter;
        }
        
        private Task runningTask;
        private T[] buffer;
        private ChannelReader<T> reader;
        
        public TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(5);

        public int NumberOfRetries { get; set; } = 5;

        public TimeSpan TimeBetweenMainLoopRestart { get; set; } = TimeSpan.FromSeconds(1);

        public void Start(ChannelReader<T> channelReader)
        {
            this.reader = channelReader;
            this.runningTask = Task.Run(this.ReadFromChannel);
        }

        private async Task ReadFromChannel()
        {
            while (true)
            {
                try
                {
                    while (await this.reader.WaitToReadAsync())
                    {
                        try
                        {
                            this.buffer = ArrayPool<T>.Shared.Rent(16);

                            T item;

                            int i = 0;
                    
                            for (; this.reader.TryRead(out item); i += 1)
                            {
                                if (i == this.buffer.Length)
                                {
                                    this.ResizeBuffer();
                                }

                                this.buffer[i] = item;
                            }

                            var segment = new ArraySegment<T>(this.buffer, 0, i);

                            Exception exception = null;
                    
                            for (int j = 0; j < this.NumberOfRetries + 1; j++)
                            {
                                try
                                {
                                    await this.ProcessBatch(segment);
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
                                await this.ErrorReporter.Error(exception);
                            }
                        }
                        finally
                        {
                            if (this.buffer != null)
                            {
                                ArrayPool<T>.Shared.Return(this.buffer);                        
                            }
                        }
                    }

                    break;
                }
                catch (Exception ex)
                {
                    await this.ErrorReporter.Error(ex);

                    await Task.Delay(this.TimeBetweenMainLoopRestart);
                }
            }
        }

        private void ResizeBuffer()
        {
            T[] newBuffer = null;

            try
            {
                newBuffer = ArrayPool<T>.Shared.Rent(this.buffer.Length * 2);
                
                Array.Copy(this.buffer, newBuffer, this.buffer.Length);
            }
            catch (Exception)
            {
                if (newBuffer != null)
                {
                    ArrayPool<T>.Shared.Return(newBuffer);                                        
                }
                                    
                throw;
            }

            ArrayPool<T>.Shared.Return(this.buffer);

            this.buffer = newBuffer;
        }

        protected abstract ValueTask ProcessBatch(ArraySegment<T> data);
        
        public virtual Task Stop() => this.runningTask;
    }

    public interface ILog
    {
        void Log<T>(string key, Func<T> func);
    }

    public class StructuredLogger : IAsyncDisposable, ILog
    {
        private readonly Action<StructuredLoggerBuilder> buildLogger;
        private readonly Dictionary<string, object> producersByConfigName;
        private HashSet<string> enabledConfigs;
        
        public StructuredLogger(Action<StructuredLoggerBuilder> buildLogger)
        {
            if (buildLogger == null)
            {
                throw new DetailedLogException("The build function is null.");
            }

            this.buildLogger = buildLogger;

            var builder = new StructuredLoggerBuilder();
            buildLogger(builder);
            
            this.producersByConfigName = builder.ConsumersByConfigName;
            this.enabledConfigs = new HashSet<string>(this.producersByConfigName.Keys);
        }

        public void Log<T>(string key, Func<T> func)
        {
            if (!this.enabledConfigs.Contains(key))
            {
                return;
            }
            
            var producer = (LogProducer<T>) this.producersByConfigName[key];

            var item = func();
            
            producer.Enqueue(item);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kvp in this.producersByConfigName)
            {
                await ((IAsyncDisposable) kvp.Value).DisposeAsync();
            }
        }

        public void SetEnabled(string[] configNames)
        {
            var newEnabledConfigs = new HashSet<string>(configNames);

            this.enabledConfigs = newEnabledConfigs;
        }
    }

    public class StructuredLoggerBuilder
    {
        public Dictionary<string, Dictionary<string, Func<object>>> LogProducerFactoryMap { get; } 
            = new Dictionary<string, Dictionary<string, Func<object>>>();
        
        public void AddConfig<T>(string configName, Dictionary<string, Func<LogConsumer<T>>> consumerFactoryMap)
        {
            if (!this.LogProducerFactoryMap.ContainsKey(configName))
            {
                this.LogProducerFactoryMap.Add(configName, new Dictionary<string, Func<object>>());
            }

            var config = this.LogProducerFactoryMap[configName];

            foreach (var (consumerName, consumerFactory) in consumerFactoryMap)
            {
                if (config.ContainsKey(consumerName))
                {
                    throw new DetailedLogException("There already is a consumer with the same name for this configuration.")
                    {
                        Details =
                        {
                            {"configName", configName},
                            {"consumerName", consumerName},
                        }
                    };
                }
                
                
            }

            
            
            config.Add(consumerName, () => new LogProducer<T>());
        }
    }

    public class StructuredLoggerBuilderConsumerMap<TLogData>
    {
        public string ConsumerName { get; set; }

        public Func<LogConsumer<T>> Func { get; set; }
    } 
    
    

    public class StructuredLoggerConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }

        public StructuredLoggerConsumerConfig[] Consumers { get; set; }
    }
    
    public class StructuredLoggerConsumerConfig
    {
        public string Name { get; set; }
        
        public bool Enabled { get; set; }
    }
}
