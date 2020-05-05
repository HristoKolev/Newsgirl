namespace Newsgirl.Shared
{
    using System;
    using System.Buffers;
    using System.Runtime.CompilerServices;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public class StructuredLogger<T> : IAsyncDisposable
    {
        private readonly LogConsumer<T>[] consumers;
        private readonly Channel<T>[] channels;
        private readonly UnboundedChannelOptions channelOptions = new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = true,
        };

        public LogLevel CurrentLevel { get; set; } = LogLevel.Debug;

        public StructuredLogger(LogConsumer<T>[] consumers)
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
        
        public void Debug(Func<T> func) => this.EnqueueEvent(func, LogLevel.Debug);
        
        public void Warn(Func<T> func) => this.EnqueueEvent(func, LogLevel.Warn);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void EnqueueEvent(Func<T> func, LogLevel logLevel)
        {
            if (logLevel < this.CurrentLevel)
            {
                return;
            }
            
            var logData = func();

            for (int i = 0; i < this.channels.Length; i++)
            {
                var channel = this.channels[i];

                if (!channel.Writer.TryWrite(logData))
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
                exitTasks[i] = this.consumers[i].WaitForCompletion();
            }

            await Task.WhenAll(exitTasks);
        }
    }
    
    public enum LogLevel
    {
        Debug,
        
        Warn,
    }

    public interface LogConsumer<T>
    {
        void Start(ChannelReader<T> channelReader);

        Task WaitForCompletion();
    }
    
    public abstract class LogConsumerBase<T> : LogConsumer<T>
    {
        private Task runningTask;
        private ChannelReader<T> reader;

        public void Start(ChannelReader<T> channelReader)
        {
            this.reader = channelReader;
            this.runningTask = Task.Run(this.ReadFromChannel);
        }

        private async Task ReadFromChannel()
        {
            while (await this.reader.WaitToReadAsync())
            {
                T[] buffer = null;

                try
                {
                    buffer = ArrayPool<T>.Shared.Rent(16);

                    T item;

                    int i = 0;
                    
                    for (; this.reader.TryRead(out item); i += 1)
                    {
                        if (i == buffer.Length)
                        {
                            buffer = ResizeBuffer(buffer);
                        }

                        buffer[i] = item;
                    }

                    await this.ProcessBatch(new ArraySegment<T>(buffer, 0, i));
                }
                finally
                {
                    if (buffer != null)
                    {
                        ArrayPool<T>.Shared.Return(buffer);                        
                    }
                }
            }
        }

        private static T[] ResizeBuffer(T[] buffer)
        {
            T[] newBuffer = null;

            try
            {
                newBuffer = ArrayPool<T>.Shared.Rent(buffer.Length * 2);
                
                Array.Copy(buffer, newBuffer, buffer.Length);
            }
            catch (Exception)
            {
                if (newBuffer != null)
                {
                    ArrayPool<T>.Shared.Return(newBuffer);                                        
                }
                                    
                throw;
            }

            ArrayPool<T>.Shared.Return(buffer);

            return newBuffer;
        }

        protected abstract ValueTask ProcessBatch(ArraySegment<T> data);
        
        public virtual Task WaitForCompletion() => this.runningTask;
    }
}
