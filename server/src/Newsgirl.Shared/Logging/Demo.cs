namespace ConsoleApp1
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Tasks;

    internal static class Program
    {
        private static void Main(string[] args)
        {
        }
    }
    
    /// <summary>
    /// TODO: define invalid config behaviour. 
    /// </summary>
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

        public ILog Build()
        {
            // The type.
            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType(
                "StructuredLogger+" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(StructuredLogger)
            );
            
            // The constructor.
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            var ctorIl = ctor.GetILGenerator();
            var baseCtor = typeof(StructuredLogger).GetConstructor(
                BindingFlags.Public | BindingFlags.FlattenHierarchy | BindingFlags.Instance, 
                null, 
                new Type[0],
                null
            );
            
            ctorIl.Emit(OpCodes.Ldarg_0);
            ctorIl.Emit(OpCodes.Call, baseCtor!);
            ctorIl.Emit(OpCodes.Ret);

            // The ILog implementation.
            typeBuilder.AddInterfaceImplementation(typeof(ILog));

            var logMethod = typeBuilder.DefineMethod(
                "Log",
                MethodAttributes.Public | MethodAttributes.Virtual | MethodAttributes.Final
            );

            var typeParameter = logMethod.DefineGenericParameters("TLogData")[0];

            logMethod.SetParameters(typeof(string), typeof(Func<>).MakeGenericType(typeParameter));
            logMethod.SetReturnType(typeof(void));

            var il = logMethod.GetILGenerator();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(StructuredLogger).GetField("consumerCollection", BindingFlags.NonPublic | BindingFlags.Instance)!);
            il.Emit(OpCodes.Ldflda, typeof(StructuredLogger).GetField("ReferenceCount", BindingFlags.NonPublic | BindingFlags.Instance)!);
            il.Emit(OpCodes.Call, typeof(Interlocked)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(x => x.Name == "Increment" &&
                            x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType()));
            il.Emit(OpCodes.Pop);

            il.BeginExceptionBlock();
            
            
            
            il.BeginFinallyBlock();
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, typeof(StructuredLogger).GetField("referenceCount", BindingFlags.NonPublic | BindingFlags.Instance)!);
            il.Emit(OpCodes.Call, typeof(Interlocked)
                .GetMethods(BindingFlags.Static | BindingFlags.Public)
                .First(x => x.Name == "Decrement" &&
                            x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType()));
            il.Emit(OpCodes.Pop);
            
            il.EndExceptionBlock();
            
            il.Emit(OpCodes.Ret);
            
            // Create an instance.
            var generatedType = typeBuilder.CreateType();
            
            var instance = (StructuredLogger) Activator.CreateInstance(generatedType);

            return instance;
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

    public abstract class StructuredLogger : IAsyncDisposable, ILog
    {
        private Dictionary<string, Func<StructuredLoggerConfig[], object>> logConsumersFactoryMap; 
        
        protected LogConsumerCollection consumerCollection;

        public async ValueTask Reconfigure(StructuredLoggerConfig[] configArray)
        {
            var map = new Dictionary<string, object>();

            foreach (var (configName, consumersFactory) in logConsumersFactoryMap)
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
                ConsumerWriters = map.Values.SelectMany(x => ((IEnumerable)x)
                    .Cast<LogConsumerLifetime>().Select(x => x.GetWriter())).ToArray()
            };
            
            await oldCollection.WaitUntilUnused();
            await oldCollection.DisposeAsync();
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
}
