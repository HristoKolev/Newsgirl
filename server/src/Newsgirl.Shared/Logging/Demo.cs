namespace ConsoleApp1
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Channels;

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

        public StructuredLogger Build()
        {
            // The type.
            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType(
                "StructuredLogger+" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(StructuredLogger)
            );
 
            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ret);

            // The Log<T> implementation.
            var logMethod = typeBuilder.DefineMethod(
                "Log",
                MethodAttributes.Public |
                MethodAttributes.ReuseSlot |
                MethodAttributes.HideBySig |
                MethodAttributes.Final |
                MethodAttributes.Virtual
            );

            var typeParameter = logMethod.DefineGenericParameters("TLogData")[0];

            logMethod.SetParameters(typeof(string), typeof(Func<>).MakeGenericType(typeParameter));
            logMethod.SetReturnType(typeof(void));

            var il = logMethod.GetILGenerator();
            var consumerCollectionLocal = il.DeclareLocal(typeof(LogConsumerCollection));
            var itemLocal = il.DeclareLocal(typeParameter);
            var arrayIndexLocal = il.DeclareLocal(typeof(int));
            
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldfld, typeof(StructuredLogger).GetField("consumerCollection", BindingFlags.NonPublic | BindingFlags.Instance)!);
            il.Emit(OpCodes.Stloc, consumerCollectionLocal);

            il.Emit(OpCodes.Ldloc, consumerCollectionLocal);
            il.Emit(OpCodes.Ldflda, typeof(LogConsumerCollection).GetField("ReferenceCount", BindingFlags.Public | BindingFlags.Instance)!);
            il.Emit(OpCodes.Call, typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x => x.Name == "Increment" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType()));
            il.Emit(OpCodes.Pop);

            il.BeginExceptionBlock();

            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(typeof(Func<>).MakeGenericType(typeParameter), typeof(Func<>).GetMethod("Invoke")!));
            il.Emit(OpCodes.Stloc, itemLocal);
            
            var loopCheckLabel = il.DefineLabel();
            var loopBodyLabel = il.DefineLabel();
            
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);
            il.Emit(OpCodes.Br_S, loopCheckLabel);
            
            // body step
            il.MarkLabel(loopBodyLabel);
            
            // Load the writer array.
            il.Emit(OpCodes.Ldloc, consumerCollectionLocal);
            il.Emit(OpCodes.Ldfld, typeof(LogConsumerCollection).GetField("ConsumerWriters")!);
            
            // Get element at index.
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldelem_Ref);

            // Use the writer.
            il.Emit(OpCodes.Ldloc, itemLocal);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(typeof(ChannelWriter<>).MakeGenericType(typeParameter), typeof(ChannelWriter<>).GetMethod("TryWrite")!));
            il.Emit(OpCodes.Pop);
            
            // Increment step.
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);
            
            // Compare step.
            il.MarkLabel(loopCheckLabel);
            
            // Load the index.
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            
            // Load the writer array.
            il.Emit(OpCodes.Ldloc, consumerCollectionLocal);
            il.Emit(OpCodes.Ldfld, typeof(LogConsumerCollection).GetField("ConsumerWriters")!);
            
            // Compare.
            il.Emit(OpCodes.Ldlen);
            il.Emit(OpCodes.Conv_I4);
            il.Emit(OpCodes.Blt_S, loopBodyLabel);
            
            il.BeginFinallyBlock();
            
            il.Emit(OpCodes.Ldloc, consumerCollectionLocal);
            il.Emit(OpCodes.Ldflda, typeof(LogConsumerCollection).GetField("ReferenceCount", BindingFlags.Public | BindingFlags.Instance)!);
            il.Emit(OpCodes.Call, typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x => x.Name == "Decrement" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType()));
            il.Emit(OpCodes.Pop);
            
            il.EndExceptionBlock();
            
            il.Emit(OpCodes.Ret);
            
            // Create an instance.
            var generatedType = typeBuilder.CreateType();
            
            var instance = (StructuredLogger) Activator.CreateInstance(generatedType);
            instance.SetFactoryMap(this.logConsumersFactoryMap);

            return instance;
        }
    }
}
