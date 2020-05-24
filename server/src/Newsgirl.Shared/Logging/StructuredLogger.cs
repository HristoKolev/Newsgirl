namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Threading.Tasks;

    public class StructuredLogger : IAsyncDisposable, ILog
    {
        private readonly Dictionary<string, Func<StructuredLoggerConfig[], object>> factoryMap;

        private LogConsumerCollection consumerCollection;

        public StructuredLogger(Dictionary<string, Func<StructuredLoggerConfig[], object>> factoryMap)
        {
            this.factoryMap = factoryMap;
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
            this.consumerCollection = LogConsumerCollection.Build(map);

            if (oldCollection != null)
            {
                await oldCollection.DisposeAsync();    
            }
        }

        public void Log<TData>(string configName, Func<TData> item)
        {
            this.consumerCollection.Log(configName, item);
        }

        public ValueTask DisposeAsync()
        {
            return this.consumerCollection.DisposeAsync();
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
