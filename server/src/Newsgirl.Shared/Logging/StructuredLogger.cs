namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;

    public abstract class StructuredLogger : IAsyncDisposable, ILog
    {
        private Dictionary<string, Func<StructuredLoggerConfig[], object>> factoryMap;

        protected LogConsumerCollection consumerCollection;

        public void SetFactoryMap(Dictionary<string, Func<StructuredLoggerConfig[], object>> map)
        {
            this.factoryMap = map;
        }
        
        public async ValueTask Reconfigure(StructuredLoggerConfig[] configArray)
        {
            var map = new Dictionary<string, object>();

            foreach (var (configName, consumersFactory) in this.factoryMap)
            {
                var consumers = consumersFactory(configArray);
                
                if (consumers == null)
                {
                    continue;
                }
                
                map.Add(configName, consumers);
            }
            
            var oldCollection = this.consumerCollection;
            
            this.consumerCollection = new LogConsumerCollection
            {
                ConsumersByConfigName = map,
                ConsumerWriters = map.Values
                    .SelectMany(x => ((IEnumerable)x).Cast<LogConsumerLifetime>())
                    .Select(x => x.GetWriter())
                    .ToArray()
            };

            if (oldCollection != null)
            {
                await oldCollection.WaitUntilUnused();
                await oldCollection.DisposeAsync();    
            }
        }
        
        public abstract void Log<TData>(string configName, Func<TData> item);

        public async ValueTask DisposeAsync()
        {
            await this.consumerCollection.WaitUntilUnused();
            await this.consumerCollection.DisposeAsync();
        }
    }
    
    public class LogConsumerCollection
    {
        public Dictionary<string, object> ConsumersByConfigName;
        
        public object[] ConsumerWriters;
        
        public int ReferenceCount;
        
        public async Task WaitUntilUnused()
        {
            while (this.ReferenceCount != 0)
            {
                await Task.Delay(100);
            }
        }
        
        public async ValueTask DisposeAsync()
        {
            var disposeTasks = new List<Task>();

            foreach (var consumersObj in this.ConsumersByConfigName.Values)
            {
                foreach (var consumer in ((IEnumerable)consumersObj).Cast<LogConsumerLifetime>())
                {
                    disposeTasks.Add(consumer.Stop());
                }
            }

            await Task.WhenAll(disposeTasks);
        }
    }
        
    public interface ILog
    {
        void Log<TData>(string configName, Func<TData> item); 
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
}
