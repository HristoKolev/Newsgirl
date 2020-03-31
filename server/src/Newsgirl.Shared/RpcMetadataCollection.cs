using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcMetadataCollection
    {
        public List<RpcHandlerMetadata> Handlers { get; set; }

        public Dictionary<Type, RpcHandlerMetadata> MetadataByRequestName { get; set; }

        public RpcHandlerMetadata GetMetadataByRequestType(Type requestType)
        {
            if (this.MetadataByRequestName.TryGetValue(requestType, out var metadata))
            {
                return metadata;
            }

            return null;
        }
        
        public static RpcMetadataCollection Build(RpcMetadataBuildParams buildParams)
        {
            var methodFlag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            
            var markedMethods = buildParams.PotentialHandlerTypes.SelectMany(type => type.GetMethods(methodFlag))
                .Where(info => info.GetCustomAttribute<RpcBindAttribute>() != null).ToList();

            var handlers = new List<RpcHandlerMetadata>(markedMethods.Count);

            foreach (var markedMethod in markedMethods)
            {
                if (markedMethod.IsStatic)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    throw new DetailedLogException($"Static methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                }
                
                if (markedMethod.IsPrivate)
                {
                    // ReSharper disable once PossibleNullReferenceException
                    throw new DetailedLogException($"Private methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                }

                var bindAttribute = markedMethod.GetCustomAttribute<RpcBindAttribute>();

                var metadata = new RpcHandlerMetadata
                {
                    HandlerClass = markedMethod.DeclaringType,
                    HandlerMethod = markedMethod,
                    RequestType = bindAttribute.RequestType,
                    ResponseType = bindAttribute.ResponseType,
                    Parameters = markedMethod.GetParameters().Select(x => x.ParameterType).ToList(),
                    SupplementalAttributes = new Dictionary<Type, RpcSupplementalAttribute>(),
                    ReturnType = markedMethod.ReturnType,
                };

                // ReSharper disable once AssignNullToNotNullAttribute
                var allSupplementalAttributes = metadata.HandlerClass.GetCustomAttributes()
                    .Concat(metadata.HandlerMethod.GetCustomAttributes())
                    .Where(x => x is RpcSupplementalAttribute)
                    .Cast<RpcSupplementalAttribute>()
                    .ToList();
                
                foreach (var supplementalAttribute in allSupplementalAttributes)
                {
                    var type = supplementalAttribute.GetType();
                    metadata.SupplementalAttributes[type] = supplementalAttribute;
                }

                var allowedParameterTypes = new List<Type> {metadata.RequestType};

                if (buildParams.HandlerArgumentTypeWhiteList != null)
                {
                    allowedParameterTypes.AddRange(buildParams.HandlerArgumentTypeWhiteList);
                }

                foreach (var parameter in metadata.Parameters)
                {
                    if (!allowedParameterTypes.Contains(parameter))
                    {
                        // ReSharper disable once PossibleNullReferenceException
                        throw new DetailedLogException($"Parameter of type {parameter.Name} is not supported for RPC methods. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                    }
                }

                if (!typeof(Task).IsAssignableFrom(metadata.ReturnType))
                {
                    throw new DetailedLogException($"Only Tasks are allowed as RPC return types. Return type: {metadata.ReturnType.Name}.");   
                }

                Type underlyingReturnType;
                
                if (metadata.ReturnType == typeof(Task))
                {
                    underlyingReturnType = typeof(void);
                }
                else
                {
                    underlyingReturnType = metadata.ReturnType.GetGenericArguments().Single();
                }

                if (underlyingReturnType != typeof(void) && underlyingReturnType != metadata.ResponseType)
                {
                    throw new DetailedLogException($"Unsupported underlying return type: Task<{underlyingReturnType.Name}>.");
                }

                metadata.UnderlyingReturnType = underlyingReturnType;

                var collidingMetadata = handlers.FirstOrDefault(x => x.RequestType == metadata.RequestType);

                if (collidingMetadata != null)
                {
                    throw new DetailedLogException(
                        "Handler binding conflict. 2 request types are bound to the same handler method. " +

                        $"{metadata.RequestType.Name} => {collidingMetadata.HandlerClass.Name}.{collidingMetadata.HandlerMethod.Name} AND " +

                        $"{metadata.RequestType.Name} => {metadata.HandlerClass.Name}.{metadata.HandlerMethod.Name}");
                }

                handlers.Add(metadata);
            }

            var metadataByRequestName = handlers.ToDictionary(x => x.RequestType, x => x);
            
            var collection = new RpcMetadataCollection
            {
                Handlers = handlers.OrderBy(x => x.RequestType.Name).ToList(),
                MetadataByRequestName = metadataByRequestName,
            };

            return collection;
        }
    }
    
    public class RpcMetadataBuildParams
    {
        public Type[] PotentialHandlerTypes { get; set; }

        public Type[] MiddlewareTypes { get; set; }
        
        public Type[] HandlerArgumentTypeWhiteList { get; set; }
    }

    public class RpcHandlerMetadata
    {
        public Type HandlerClass { get; set; }

        public MethodInfo HandlerMethod { get; set; }
        
        public Type RequestType { get; set; }
        
        public Type ResponseType { get; set; }
        
        public List<Type> Parameters { get; set; }
        
        public Dictionary<Type, RpcSupplementalAttribute> SupplementalAttributes { get; set; }
        
        public Type ReturnType { get; set; }

        public Type UnderlyingReturnType { get; set; }
        
        public Func<object, object, InstanceProvider, Task> CompiledHandler { get; set; }
    }
    
    /// <summary>
    /// This is used to mark methods as RPC handlers.
    /// </summary>
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public class RpcBindAttribute : Attribute
    {
        public Type RequestType { get; }
        
        public Type ResponseType { get; }

        public RpcBindAttribute(Type requestType, Type responseType)
        {
            this.RequestType = requestType;
            this.ResponseType = responseType;
        }
    }
    
    /// <summary>
    /// This is the base type for all supplemental attributes
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method)]
    public abstract class RpcSupplementalAttribute : Attribute
    {
    }
}