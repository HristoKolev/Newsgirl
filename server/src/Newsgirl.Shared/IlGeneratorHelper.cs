namespace Newsgirl.Shared
{
    using System;
    using System.Reflection;
    using System.Reflection.Emit;

    public static class ReflectionEmmitHelper
    {
        private static bool initialized;
        private static readonly object SyncRoot = new object();
        private static ModuleBuilder moduleBuilder;

        private static void Initialize()
        {
            if (initialized)
            {
                return;
            }

            lock (SyncRoot)
            {
                if (initialized)
                {
                    return;
                }

                var assemblyName = new AssemblyName("DynamicAssembly+" + nameof(ReflectionEmmitHelper));
                var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
                moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name!);

                initialized = true;
            }
        }

        public static ModuleBuilder ModuleBuilder
        {
            get
            {
                Initialize();
                return moduleBuilder;
            }
        }

        public static T CreateDelegate<T>(this MethodInfo methodInfo) where T : Delegate
        {
            return (T) methodInfo.CreateDelegate(typeof(T));
        }
    }
}
