namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Collections.Generic;
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
        private Dictionary<string, object> producersByConfigName;

        public StructuredLogger(Action<StructuredLoggerBuilder> buildLogger)
        {
            this.buildLogger = buildLogger ?? throw new DetailedLogException("The build function is null.");
        }

        public void Log<T>(string key, Func<T> func)
        {
            var map = this.producersByConfigName;

            if (!map.TryGetValue(key, out var producerObj))
            {
                return;
            }

            var item = func();
            
            ((LogProducer<T>)producerObj).Enqueue(item);
        }

        public Task Reconfigure(StructuredLoggerConfig[] configArray)
        {
            var builder = new StructuredLoggerBuilder();
            this.buildLogger(builder);
            
            var map = new Dictionary<string, object>();

            foreach (var (configName, producerFactory) in builder.LogProducerFactoryMap)
            {
                if (producerFactory == null)
                {
                    continue;
                }
                
                map.Add(configName, producerFactory(configArray));
            }

            var oldMap = this.producersByConfigName;
            this.producersByConfigName = map;
            return DisposeProducers(oldMap);
        }

        private static Task DisposeProducers(Dictionary<string, object> producerMap)
        {
            var disposeTasks = new List<Task>();

            foreach (var kvp in producerMap)
            {
                if (kvp.Value is IAsyncDisposable x)    
                {
                    disposeTasks.Add(x.DisposeAsync().AsTask());
                }
            }

            return Task.WhenAll(disposeTasks);
        }

        public async ValueTask DisposeAsync()
        {
            await DisposeProducers(this.producersByConfigName);
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
        
        // ReSharper disable once MemberCanBeProtected.Global
        public TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(5);

        // ReSharper disable once MemberCanBeProtected.Global
        public int NumberOfRetries { get; set; } = 5;

        // ReSharper disable once MemberCanBeProtected.Global
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
}
