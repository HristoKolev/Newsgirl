using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq.Expressions;
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
        public const int IterationCount = 1_000_000;
        
        public static async Task Main()
        {
            await CompiledExpressions();
            
            await InlinedCode();

            Console.WriteLine("===================");
            
            await CompiledExpressions();
            
            await InlinedCode();
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
            Example.Cats += 1;
            return Task.CompletedTask;
        }
    }

    public class Mid1 : IMid
    {
        public Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Example.Cats += 1;
            return next(context);
        }
    }
    
    public class Mid2 : IMid
    {
        public Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Example.Cats += 1;
            return next(context);
        }
    }
    
    public class Mid3 : IMid
    {
        public Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Example.Cats += 1;
            return next(context);
        }
    }
    
    public class Mid4 : IMid
    {
        public Task Run(RpcContext context, RpcRequestDelegate next)
        {
            Example.Cats += 1;
            return next(context);
        }
    }

    public interface IMid
    {
        Task Run(RpcContext context, RpcRequestDelegate next);
    }
}
