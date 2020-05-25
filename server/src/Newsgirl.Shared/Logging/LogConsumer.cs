namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Buffers;
    using System.Threading.Channels;
    using System.Threading.Tasks;
    using Shared;

    /// <summary>
    /// Base class for all log consumers.
    /// </summary>
    public abstract class LogConsumer<TData> : LogConsumerControl
    {
        private readonly ErrorReporter errorReporter;
        private Task readTask;
        private TData[] buffer;
        private bool started;

        protected LogConsumer(ErrorReporter errorReporter)
        {
            this.errorReporter = errorReporter;
        }
        
        protected TimeSpan TimeBetweenRetries { get; set; } = TimeSpan.FromSeconds(5);

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