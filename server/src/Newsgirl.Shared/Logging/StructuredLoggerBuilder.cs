namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public class StructuredLoggerBuilder
    {
        private readonly Dictionary<string, Func<StructuredLoggerConfig[], object>> logConsumersFactoryMap
            = new Dictionary<string, Func<StructuredLoggerConfig[], object>>(); 
        
        public void AddConfig<T>(string configName, Dictionary<string, Func<LogConsumer<T>>> consumerFactoryMap)
        {
            if (this.logConsumersFactoryMap.ContainsKey(configName))
            {
                throw new DetailedLogException("There already is a configuration with this name.")
                {
                    Details =
                    {
                        {"configName", configName},
                    }
                };
            }

            this.logConsumersFactoryMap.Add(configName, configArray =>
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

        public StructuredLogger Build()
        {
            return new StructuredLogger(this.logConsumersFactoryMap);
        }
    }
}
