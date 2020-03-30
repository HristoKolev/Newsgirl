using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Threading.Tasks;

namespace RpcPlay
{
    public class RpcContext
    {
        private Task responseTask;
        
        public object Request { get; set; }

        public Dictionary<Type, object> Items { get; set; }

        public Task ResponseTask
        {
            get => this.responseTask;
            set => this.responseTask = value;
        }

        public void SetResponse<T>(T obj)
        {
            this.responseTask = Task.FromResult(obj);
        }
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
    
    public interface RpcMiddleware
    {
        Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next);
    }

    public delegate Task RpcRequestDelegate(RpcContext context, InstanceProvider instanceProvider);
    
    public static class Example
    {
        public static async Task Main()
        {
            // await InlinedCode();
            
            await IlGeneratedCode();

        }

        private static async Task IlGeneratedCode()
        {
            var context = new RpcContext
            {
                Request = new SimpleRequest1(),
            };

            var instanceProvider = new InstanceProvider();

            var midTypes = new[]
            {
                typeof(Mid1),
                typeof(Mid2),
                typeof(Mid3),
                typeof(Mid4),
            };

            var handlerType = typeof(TestHandler);
            var requestType = typeof(SimpleRequest1);
            var handlerMethod = typeof(TestHandler).GetMethod("RpcMethod");
            var handlerMethodParameters = handlerMethod.GetParameters().Select(x => x.ParameterType).ToList();

            var dictionaryGetItem = typeof(Dictionary<Type, object>).GetMethod("get_Item");
            
            // force create
            foreach (var type in midTypes)
            {
                instanceProvider.Get(type);
            }

            instanceProvider.Get(typeof(TestHandler));

            var getInstance = typeof(InstanceProvider).GetMethod("Get", new[] {typeof(Type)});

            var typeBuilder = IlGeneratorHelper.ModuleBuilder.DefineType("RpcMiddlewareDynamicType+" + Guid.NewGuid(),
                TypeAttributes.Class | TypeAttributes.Public | TypeAttributes.Sealed | TypeAttributes.Abstract | TypeAttributes.AutoClass | TypeAttributes.AnsiClass);

            var delegateFieldMap = new Dictionary<MethodInfo, FieldBuilder>();

            var executeHandler = typeBuilder.DefineMethod(
                "executeHandlerMethod",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(Task),
                new[] {typeof(RpcContext), typeof(InstanceProvider)}
            );
            
            var executeHandlerGen = executeHandler.GetILGenerator();
            
            executeHandlerGen.DeclareLocal(typeof(Task));
            
            // load the handler instance from the instanceProvider
            executeHandlerGen.Emit(OpCodes.Ldarg_1);
            executeHandlerGen.Emit(OpCodes.Ldtoken, handlerType);
            executeHandlerGen.Emit(OpCodes.Call, getInstance);

            foreach (var parameter in handlerMethodParameters)
            {
                if (parameter == requestType)
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("Request").GetMethod);
                }
                else
                {
                    executeHandlerGen.Emit(OpCodes.Ldarg_0);
                    executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("Items").GetMethod);
                    executeHandlerGen.Emit(OpCodes.Ldtoken, parameter);
                    executeHandlerGen.Emit(OpCodes.Call, dictionaryGetItem);
                }
            }
                    
            executeHandlerGen.Emit(OpCodes.Call, handlerMethod);
            executeHandlerGen.Emit(OpCodes.Stloc_0);
            
            executeHandlerGen.Emit(OpCodes.Ldarg_0);
            executeHandlerGen.Emit(OpCodes.Ldloc_0);
            executeHandlerGen.Emit(OpCodes.Call, typeof(RpcContext).GetProperty("ResponseTask").SetMethod);
                    
            executeHandlerGen.Emit(OpCodes.Ldloc_0);
            executeHandlerGen.Emit(OpCodes.Ret);

            MethodInfo lastMethod = executeHandler;
            delegateFieldMap.Add(executeHandler, typeBuilder.DefineField("executeHandlerDelegateField", typeof(RpcRequestDelegate), FieldAttributes.Private | FieldAttributes.Static));

            for (int i = midTypes.Length - 1; i >= 0; i--)
            {
                var midType = midTypes[i];
                
                var currentMethod = typeBuilder.DefineMethod(
                    "middlewareRun" + i,
                    MethodAttributes.Private | MethodAttributes.Static,
                    typeof(Task),
                    new[] {typeof(RpcContext), typeof(InstanceProvider)}
                );

                var gen = currentMethod.GetILGenerator();

                // load the middleware instance from the instanceProvider
                gen.Emit(OpCodes.Ldarg_1);
                gen.Emit(OpCodes.Ldtoken, midType);
                gen.Emit(OpCodes.Call, getInstance);
                
                // load the context
                gen.Emit(OpCodes.Ldarg_0);
                
                // load the instanceProvider
                gen.Emit(OpCodes.Ldarg_1);
                
                // load the next delegate
                gen.Emit(OpCodes.Ldsfld, delegateFieldMap[lastMethod]);
                
                // call the middleware RUN function
                gen.Emit(OpCodes.Call, midType.GetMethod("Run"));
                
                gen.Emit(OpCodes.Ret);
                
                lastMethod = currentMethod;
                delegateFieldMap.Add(currentMethod, typeBuilder.DefineField("middlewareDelegateField" + i, typeof(RpcRequestDelegate), FieldAttributes.Private | FieldAttributes.Static));
            }
            
            // initialize the delegate fields
            var initializeDelegateFieldsMethod = typeBuilder.DefineMethod(
                "initializeDelegateFields",
                MethodAttributes.Private | MethodAttributes.Static,
                typeof(void),
                Array.Empty<Type>()
            );

            var initializeDelegateIlGen = initializeDelegateFieldsMethod.GetILGenerator();

            foreach (var kvp in delegateFieldMap)
            {
                initializeDelegateIlGen.Emit(OpCodes.Ldnull);
                initializeDelegateIlGen.Emit(OpCodes.Ldftn, kvp.Key);
                initializeDelegateIlGen.Emit(OpCodes.Newobj, typeof(RpcRequestDelegate).GetConstructors()[0]);
                initializeDelegateIlGen.Emit(OpCodes.Stsfld, kvp.Value);
            }
            
            initializeDelegateIlGen.Emit(OpCodes.Ret);
            
            var dynamicType = typeBuilder.CreateType();

            var methodInfo = dynamicType.GetMethod(lastMethod.Name, BindingFlags.NonPublic | BindingFlags.Static);
            var run = (RpcRequestDelegate) methodInfo
                .CreateDelegate(typeof(RpcRequestDelegate));

            var initializeDelegateFields = (Action) dynamicType.GetMethod("initializeDelegateFields", BindingFlags.NonPublic | BindingFlags.Static)
                .CreateDelegate(typeof(Action));

            initializeDelegateFields();

            await run(context, instanceProvider);

            Console.WriteLine(context);
        }

        private static async Task InlinedCode()
        {
            var context = new RpcContext
            {
                Request = new SimpleRequest1(),
            };
            
            var instanceProvider = new InstanceProvider();
            
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
                instanceProvider.Get(type);
            }
            
            await InlineFunc(context, instanceProvider);
        }

        private static Task InlineFunc(RpcContext context, InstanceProvider instanceProvider)
        {
            return ((Mid1)instanceProvider.Get(typeof(Mid1)))
                .Run(context, instanceProvider, (ctx4, instanceProvider4) => ((Mid2) instanceProvider4.Get(typeof(Mid2)))
                .Run(ctx4, instanceProvider4, (ctx3, instanceProvider3) => ((Mid3) instanceProvider3.Get(typeof(Mid3)))
                .Run(ctx3, instanceProvider3, (ctx2, instanceProvider2) => ((Mid4) instanceProvider2.Get(typeof(Mid4)))
                .Run(ctx2, instanceProvider2, (ctx1, instanceProvider1) =>
                    {
                        return ((TestHandler) instanceProvider1.Get(typeof(TestHandler))).RpcMethod(
                            (SimpleRequest1)ctx1.Request);
                    }))));
        }
    }

    public class Mid1 : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            Console.WriteLine(1); 
            await next(context, instanceProvider);
            Console.WriteLine(8);
        }
    }
    
    public class Mid2 : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            Console.WriteLine(2); 
            await next(context, instanceProvider);
            Console.WriteLine(7);
        }
    }
    
    public class Mid3 : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            Console.WriteLine(3); 
            await next(context, instanceProvider);
            Console.WriteLine(6);
        }
    }
    
    public class Mid4 : RpcMiddleware
    {
        public async Task Run(RpcContext context, InstanceProvider instanceProvider, RpcRequestDelegate next)
        {
            Console.WriteLine(4); 
            await next(context, instanceProvider);
            Console.WriteLine(5);
        }
    }

    public class TestHandler
    {
        public async Task<SimpleResponse1> RpcMethod(SimpleRequest1 req)
        {
            return new SimpleResponse1
            {
                Num = req.Num + 1
            };
        }
    }

    public class SimpleRequest1
    {
        public int Num { get; set; }    
    }

    public class SimpleResponse1
    {
        public int Num { get; set; }
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
