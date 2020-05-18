namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
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

            public void IncrementRc() => Interlocked.Increment(ref this.referenceCount);

            public void DecrementRc() => Interlocked.Decrement(ref this.referenceCount);

            public async Task WaitUntilUnused()
            {
                while (this.referenceCount != 0)
                {
                    await Task.Delay(100);
                }
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


}
