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
#if DEBUG
        private readonly TimeSpan timeBetweenRetry = TimeSpan.Zero;
#else
        private readonly TimeSpan timeBetweenRetry = TimeSpan.FromSeconds(5);
#endif

        public void Start(ChannelReader<T> channelReader)
        {
            this.reader = channelReader;
            this.runningTask = Task.Run(this.ReadFromChannel);
        }

        private async Task ReadFromChannel()
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

                    for (int j = 0; j < 5; j++)
                    {
                        try
                        {
                            await this.ProcessBatch(segment);
                            
                            break;
                        }
                        catch (Exception exception)
                        {
                            await this.ErrorReporter.Error(exception);
                            
                            await Task.Delay(this.timeBetweenRetry);
                        }
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
        private readonly Dictionary<string, object> consumersByConfigName;
        private HashSet<string> enabledConfigs;
        
        public StructuredLogger(Action<StructuredLoggerBuilder> configure)
        {
            if (configure == null)
            {
                throw new DetailedLogException("The configure function is null.");
            }
            
            var builder = new StructuredLoggerBuilder();
            configure(builder);
            
            this.consumersByConfigName = builder.ConsumersByConfigName;
            this.enabledConfigs = new HashSet<string>(this.consumersByConfigName.Keys);
        }

        public void Log<T>(string key, Func<T> func)
        {
            if (!this.enabledConfigs.Contains(key))
            {
                return;
            }
            
            var producer = (LogProducer<T>) this.consumersByConfigName[key];

            var item = func();
            
            producer.Enqueue(item);
        }

        public async ValueTask DisposeAsync()
        {
            foreach (var kvp in this.consumersByConfigName)
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
        public Dictionary<string, object> ConsumersByConfigName { get; } = new Dictionary<string, object>();
        
        public void AddConfig<T>(string configName, LogConsumer<T>[] consumers)
        {
            this.ConsumersByConfigName.Add(configName, new LogProducer<T>(consumers));
        }
    }
}
