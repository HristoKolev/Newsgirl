using System;
using System.Reflection;
using System.Reflection.Emit;

namespace BuildingTypes
{
    public delegate int Add42(int arg1);
    
    public class Program
    {
        static void Main(string[] args)
        {
            var typeBuilder = ILGeneratorHelper.ModuleBuilder.DefineType("RpcMiddlewareDynamicType+" + Guid.NewGuid(),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

            var d1 = typeBuilder.DefineMethod(
                "d1",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(int),
                new[] {typeof(int)}
            );

            var d1Generator = d1.GetILGenerator();

            d1Generator.Emit(OpCodes.Ldarg_0);
            d1Generator.Emit(OpCodes.Ldc_I4, 42);
            d1Generator.Emit(OpCodes.Add);
            d1Generator.Emit(OpCodes.Ret);
            
            var d2 = typeBuilder.DefineMethod(
                "d2",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(int),
                new[] {typeof(Add42)}
            );

            var d2Generator = d2.GetILGenerator();

            d2Generator.Emit(OpCodes.Ldarg_0);
            d2Generator.Emit(OpCodes.Ldc_I4, 8);
            d2Generator.CallDelegate<Add42>();
            d2Generator.Emit(OpCodes.Ret);
            
            var d3 = typeBuilder.DefineMethod(
                "d3",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(int), Array.Empty<Type>()
            );

            var d3Generator = d3.GetILGenerator();
            d3Generator.LoadDelegate<Add42>(d1);
            d3Generator.Emit(OpCodes.Call, d2);
            d3Generator.Emit(OpCodes.Ret);
            
            var type = typeBuilder.CreateType();
            var result = (Func<int>) type.GetMethod("d3", BindingFlags.NonPublic | BindingFlags.Static).CreateDelegate(typeof(Func<int>));

            Console.WriteLine(result());
        }
    }

    public static class ILGeneratorExtensions
    {
        public static void LoadDelegate<T>(this ILGenerator ilGenerator, MethodInfo methodInfo) where T: Delegate
        {
            ilGenerator.Emit(OpCodes.Ldnull);
            ilGenerator.Emit(OpCodes.Ldftn, methodInfo);
            ilGenerator.Emit(OpCodes.Newobj, typeof(T).GetConstructors()[0]);
        }

        public static void CallDelegate<T>(this ILGenerator ilGenerator) where T : Delegate
        {
            ilGenerator.Emit(OpCodes.Call, typeof(T).GetMethod("Invoke"));
        }
    }

    public static class ILGeneratorHelper
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
                if (!initialized)
                {
                    var assemblyName = new AssemblyName("DynamicAssembly+" + Guid.NewGuid());
                    var assemblyBuilder = AssemblyBuilder.DefineDynamicAssembly(assemblyName, AssemblyBuilderAccess.RunAndCollect);
                    moduleBuilder = assemblyBuilder.DefineDynamicModule(assemblyName.Name);

                    initialized = true;
                }
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
    }
}