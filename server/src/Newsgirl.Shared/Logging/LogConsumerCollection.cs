namespace Newsgirl.Shared.Logging
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Threading;
    using System.Threading.Channels;
    using System.Threading.Tasks;

    public abstract class LogConsumerCollection
    {
        private Dictionary<string, object> consumersByConfigName;

        // ReSharper disable once MemberCanBePrivate.Global
        // ReSharper disable once FieldCanBeMadeReadOnly.Global
        // ReSharper disable once ConvertToConstant.Global
        protected int ReferenceCount = 0;
        
        public async ValueTask DisposeAsync()
        {
            while (this.ReferenceCount != 0)
            {
                await Task.Delay(10);
            }
            
            var disposeTasks = new List<Task>();

            foreach (var consumersObj in this.consumersByConfigName.Values)
            {
                foreach (var consumer in ((IEnumerable)consumersObj).Cast<LogConsumerControl>())
                {
                    disposeTasks.Add(consumer.Stop());
                }
            }

            await Task.WhenAll(disposeTasks);
        }

        public abstract void Log<TData>(string configName, Func<TData> item);
        
        public static LogConsumerCollection Build(Dictionary<string, object> map)
        {
            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType(
                nameof(LogConsumerCollection) + "+" + Guid.NewGuid(),
                TypeAttributes.Public | TypeAttributes.Class,
                typeof(LogConsumerCollection)
            );

            var ctor = typeBuilder.DefineConstructor(MethodAttributes.Public, CallingConventions.Standard, null);
            var ctorIl = ctor.GetILGenerator();
            ctorIl.Emit(OpCodes.Ret);
            
            EmitLog(typeBuilder, map);

            var type = typeBuilder.CreateType();

            var instance = (LogConsumerCollection) Activator.CreateInstance(type);
            
            foreach (var (configName, consumersObj) in map)
            {
                var array = ((IEnumerable) consumersObj)
                    .Cast<LogConsumerControl>()
                    .Select(consumer => consumer.GetWriter())
                    .ToArray();

                var field = type.GetField(configName + "_writers", BindingFlags.Public | BindingFlags.Instance);
                field!.SetValue(instance, array);
            }

            instance!.consumersByConfigName = map;

            return instance;
        }

        private static void EmitLog(TypeBuilder typeBuilder, Dictionary<string, object> map)
        {
            // Interlocked.Increment
            var incrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == "Increment" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());
            
            // Interlocked.Decrement
            var decrementMethod = typeof(Interlocked).GetMethods(BindingFlags.Static | BindingFlags.Public).First(x =>
                x.Name == "Decrement" && x.GetParameters().Single().ParameterType == typeof(int).MakeByRefType());
            
            // LogConsumerCollection.ReferenceCount
            var referenceCountField = typeof(LogConsumerCollection).GetField("ReferenceCount", BindingFlags.NonPublic | BindingFlags.Instance)!;
            
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
            var itemLocal = il.DeclareLocal(typeParameter);
            var arrayIndexLocal = il.DeclareLocal(typeof(int));
            var writerArrayLocal = il.DeclareLocal(typeof(object[]));

            var exitLabel = il.DefineLabel();
            var afterWritersSelect = il.DefineLabel();
            
            foreach (string configName in map.Keys)
            {
                var field = typeBuilder.DefineField(configName + "_writers", typeof(object[]), FieldAttributes.Public);

                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldstr, configName);
                il.Emit(OpCodes.Call, typeof(string).GetMethod("op_Equality")!);

                var nextLabel = il.DefineLabel();
                il.Emit(OpCodes.Brfalse_S, nextLabel);

                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldfld, field);
                il.Emit(OpCodes.Stloc, writerArrayLocal);

                il.Emit(OpCodes.Br_S, afterWritersSelect);

                il.MarkLabel(nextLabel);
            }
            
            il.Emit(OpCodes.Br_S, exitLabel);

            il.MarkLabel(afterWritersSelect);
            
            // Interlocked.Increment(ref this.ReferenceCount);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, referenceCountField!);
            il.Emit(OpCodes.Call, incrementMethod);
            il.Emit(OpCodes.Pop);

            il.BeginExceptionBlock();

            // itemLocal = arg2.Invoke()
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                typeof(Func<>).MakeGenericType(typeParameter),
                typeof(Func<>).GetMethod("Invoke")!)
            );
            il.Emit(OpCodes.Stloc, itemLocal);

            var loopCheckLabel = il.DefineLabel();
            var loopBodyLabel = il.DefineLabel();

            // Declare index local
            il.Emit(OpCodes.Ldc_I4_0);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);
            il.Emit(OpCodes.Br_S, loopCheckLabel);

            // Body step.
            il.MarkLabel(loopBodyLabel);

            // Load the writer. writerArrayLocal[i]
            il.Emit(OpCodes.Ldloc, writerArrayLocal);
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldelem_Ref);

            // Call TryWrite.
            il.Emit(OpCodes.Ldloc, itemLocal);
            il.Emit(OpCodes.Callvirt, TypeBuilder.GetMethod(
                typeof(ChannelWriter<>).MakeGenericType(typeParameter),
                typeof(ChannelWriter<>).GetMethod("TryWrite")!
            ));
            il.Emit(OpCodes.Pop);
            
            // TODO: throw if false 

            // Increment step. i++
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stloc, arrayIndexLocal);

            // Compare step.
            il.MarkLabel(loopCheckLabel);

            // Load the index local.
            il.Emit(OpCodes.Ldloc, arrayIndexLocal);

            // writerArrayLocal.Length
            il.Emit(OpCodes.Ldloc, writerArrayLocal);
            il.Emit(OpCodes.Ldlen);

            // Compare.
            il.Emit(OpCodes.Blt_S, loopBodyLabel);

            il.BeginFinallyBlock();
            
            // Interlocked.Decrement(ref this.ReferenceCount);
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, referenceCountField!);
            il.Emit(OpCodes.Call, decrementMethod);
            il.Emit(OpCodes.Pop);

            il.EndExceptionBlock();

            il.MarkLabel(exitLabel);

            il.Emit(OpCodes.Ret);
        }
    }
}
