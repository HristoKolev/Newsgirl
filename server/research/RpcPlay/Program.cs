using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace RpcPlay
{
    public class RpcContext
    {
        public RequestModel Request { get; set; }

        public InstanceProvider InstanceProvider { get; set; }
    }

    public class RequestModel
    {
    }
    
    public class InstanceProvider
    {
        private static readonly Dictionary<Type, object> Instances = new Dictionary<Type, object>();
        
        public object Get(Type type)
        {
            if (Instances.ContainsKey(type))
            {
                return Instances[type];
            }

            var instance = Activator.CreateInstance(type);

            Instances.Add(type, instance);

            return instance;
        }
    }

    public delegate Task RpcRequestDelegate(RpcContext context);

    public static class Example
    {
        public static int Cats = 1;
        public const int IterationCount = 10_000_000;

        public static async Task Main()
        {
            
     
            // await CompiledExpressions();
            //
            // await IlGeneratedCode();
            //
            // await InlinedCode();
            //
            // Console.WriteLine("===================");
            //
            // await CompiledExpressions();
            //
            // await IlGeneratedCode();
            //
            // await InlinedCode();
            
            var context = new RpcContext
            {
                Request = new RequestModel(),
                InstanceProvider = new InstanceProvider(),
            };

            var midTypes = new[]
            {
                typeof(Mid1),
                typeof(Mid2),
                typeof(Mid3),
                typeof(Mid4),
            };

            // force create
            foreach (var type in midTypes)
            {
                context.InstanceProvider.Get(type);
            }

            var getInstanceProvider = typeof(RpcContext).GetProperty("InstanceProvider").GetMethod;
            var getInstance = typeof(InstanceProvider).GetMethod("Get");

            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType("RpcMiddlewareDynamicType+" + Guid.NewGuid(),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

            MethodInfo lastMethod = typeof(Example).GetMethod("RunCode");

            for (int i = midTypes.Length - 1; i >= 0; i--)
            {
                var midType = midTypes[i];
                
                var currentMethod = typeBuilder.DefineMethod(
                    "mid" + i,
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(Task),
                    new[] {typeof(RpcContext)}
                );

                var gen = currentMethod.GetILGenerator();

                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, getInstanceProvider);
                gen.Emit(OpCodes.Ldtoken, midType);
                gen.Emit(OpCodes.Call, getInstance);
                gen.Emit(OpCodes.Ldarg_0);
                gen.LoadDelegate<RpcRequestDelegate>(lastMethod);
                gen.Emit(OpCodes.Call, midType.GetMethod("Run"));
                gen.Emit(OpCodes.Ret);

                lastMethod = currentMethod;
            }
            
            var dynamicType = typeBuilder.CreateType();
            var func = (RpcRequestDelegate) dynamicType.GetMethod("mid0", BindingFlags.NonPublic | BindingFlags.Static)
                .CreateDelegate(typeof(RpcRequestDelegate));

            await func(context);
        }

        private static async Task IlGeneratedCode()
        {
            var context = new RpcContext
            {
                Request = new RequestModel(),
                InstanceProvider = new InstanceProvider(),
            };

            var midTypes = new[]
            {
                typeof(Mid1),
                typeof(Mid2),
                typeof(Mid3),
                typeof(Mid4),
            };

            // force create
            foreach (var type in midTypes)
            {
                context.InstanceProvider.Get(type);
            }

            var getInstanceProvider = typeof(RpcContext).GetProperty("InstanceProvider").GetMethod;
            var getInstance = typeof(InstanceProvider).GetMethod("Get");

            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType("RpcMiddlewareDynamicType+" + Guid.NewGuid(),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract);

            MethodInfo lastMethod = typeof(Example).GetMethod("RunCode");

            for (int i = midTypes.Length - 1; i >= 0; i--)
            {
                var midType = midTypes[i];
                
                var currentMethod = typeBuilder.DefineMethod(
                    "mid" + i,
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(Task),
                    new[] {typeof(RpcContext)}
                );

                var gen = currentMethod.GetILGenerator();

                gen.Emit(OpCodes.Ldarg_0);
                gen.Emit(OpCodes.Call, getInstanceProvider);
                gen.Emit(OpCodes.Ldtoken, midType);
                gen.Emit(OpCodes.Call, getInstance);
                gen.Emit(OpCodes.Ldarg_0);
                gen.LoadDelegate<RpcRequestDelegate>(lastMethod);
                gen.Emit(OpCodes.Call, midType.GetMethod("Run"));
                gen.Emit(OpCodes.Ret);

                lastMethod = currentMethod;
            }
            
            var dynamicType = typeBuilder.CreateType();
            var func = (RpcRequestDelegate) dynamicType.GetMethod("mid0", BindingFlags.NonPublic | BindingFlags.Static)
                .CreateDelegate(typeof(RpcRequestDelegate));

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Example.IterationCount; i++)
            {
                await func(context);
            }
            
            sw.Stop();
            
            Console.WriteLine($"ILGENERATED FUNC: {sw.ElapsedMilliseconds}");
        }

        private static async Task InlinedCode()
        {
            var context = new RpcContext
            {
                Request = new RequestModel(),
                InstanceProvider = new InstanceProvider(),
            };
            
            var midTypes = new[]
            {
                typeof(Mid1),
                typeof(Mid2),
                typeof(Mid3),
                typeof(Mid4),
            };

            // force create
            foreach (var type in midTypes)
            {
                context.InstanceProvider.Get(type);
            }
            
            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Example.IterationCount; i++)
            {
                await InlineFunc(context);
            }
            
            sw.Stop();
            
            Console.WriteLine($"INLINE FUNC: {sw.ElapsedMilliseconds}");
        }

        private static Task InlineFunc(RpcContext context)
        {
            return ((Mid4)context.InstanceProvider.Get(typeof(Mid4)))
                .Run(context, ctx4 => ((Mid3) ctx4.InstanceProvider.Get(typeof(Mid3)))
                    .Run(ctx4, ctx3 => ((Mid2) ctx3.InstanceProvider.Get(typeof(Mid2)))
                        .Run(ctx3, ctx2 => ((Mid1) ctx2.InstanceProvider.Get(typeof(Mid1)))
                            .Run(ctx2, rpcContext => RunCode(rpcContext)))));
        }

        private static async Task CompiledExpressions()
        {
            var context = new RpcContext
            {
                Request = new RequestModel(),
                InstanceProvider = new InstanceProvider(),
            };

            var midTypes = new[]
            {
                typeof(Mid1),
                typeof(Mid2),
                typeof(Mid3),
                typeof(Mid4),
            };

            // force create
            foreach (var type in midTypes)
            {
                context.InstanceProvider.Get(type);
            }

            var getInstance = typeof(InstanceProvider).GetMethod("Get");

            var contextParam = Expression.Parameter(typeof(RpcContext), "context");

            var lambdaExpression = Expression.Lambda<RpcRequestDelegate>(
                Expression.Call(typeof(Example).GetMethod("RunCode"), contextParam), contextParam);

            for (int i = midTypes.Length - 1; i >= 0; i--)
            {
                var midType = midTypes[i];

                var localContextParam = Expression.Parameter(typeof(RpcContext), "context" + i);
                var instanceProviderExpr = Expression.Property(localContextParam, "InstanceProvider");
                var getCall = Expression.Call(instanceProviderExpr, getInstance, Expression.Constant(midType));

                var localBody = Expression.Call(
                    Expression.Convert(getCall, midType),
                    midType.GetMethod("Run"),
                    localContextParam,
                    lambdaExpression
                );

                lambdaExpression = Expression.Lambda<RpcRequestDelegate>(localBody, localContextParam);
            }

            var func = lambdaExpression.Compile();

            var sw = Stopwatch.StartNew();

            for (int i = 0; i < Example.IterationCount; i++)
            {
                await func(context);
            }
            
            sw.Stop();
            
            Console.WriteLine($"COMPILED FUNC: {sw.ElapsedMilliseconds}");
        }

        public static Task RunCode(RpcContext context)
        {
            Console.WriteLine("RUN CODE...");
            return Task.CompletedTask;
        }
    }

    public class Mid1 : IMid
    {
        public async Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Console.WriteLine(1);

            await next(context);
            
            Console.WriteLine(8);
        }
    }
    
    public class Mid2 : IMid
    {
        public async Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Console.WriteLine(2);

            await next(context);
            
            Console.WriteLine(7);
        }
    }
    
    public class Mid3 : IMid
    {
        public async Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Console.WriteLine(3);

            await next(context);
            
            Console.WriteLine(6);
        }
    }
    
    public class Mid4 : IMid
    {
        public async Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Console.WriteLine(4);

            await next(context);
            
            Console.WriteLine(5);
        }
    }

    public interface IMid
    {
        Task Run(RpcContext context, RpcRequestDelegate next);
    }
    
    public static class IlGeneratorHelper
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
}
