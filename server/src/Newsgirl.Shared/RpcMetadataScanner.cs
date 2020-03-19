using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
    public class RpcMetadataScanner
    {
        public List<RpcHandlerMetadata> ScanTypes(IEnumerable<Type> types)
        {
            var methodFlag = BindingFlags.Instance | BindingFlags.Public | BindingFlags.Static | BindingFlags.NonPublic;
            
            var markedMethods = types.SelectMany(type => type.GetMethods(methodFlag))
                .Where(info => info.GetCustomAttribute<RpcBindAttribute>() != null).ToList();

            var metadataList = new List<RpcHandlerMetadata>(markedMethods.Count);

            foreach (var markedMethod in markedMethods)
            {
                if (markedMethod.IsStatic)
                {
                    throw new DetailedLogException($"Static methods cannot be bound as an RPC handlers. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");
                }
                
                if (markedMethod.IsPrivate)
                {
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
                    SupplementalAttributes = new Dictionary<Type, RpcSupplementalAttribute>()
                };

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
                
                foreach (var parameter in metadata.Parameters)
                {
                    if (parameter != metadata.RequestType)
                    {
                        throw new DetailedLogException($"Parameter of type {parameter.Name} is not supported for RPC methods. {markedMethod.DeclaringType.Name}.{markedMethod.Name}");  
                    }
                }
                
                var collidingMetadata = metadataList.FirstOrDefault(x => x.RequestType == metadata.RequestType);

                if (collidingMetadata != null)
                {
                    throw new DetailedLogException(
                        "Handler binding conflict. 2 request types are bound to the same handler method. " +

                        $"{metadata.RequestType.Name} => {collidingMetadata.HandlerClass.Name}.{collidingMetadata.HandlerMethod.Name} AND " +

                        $"{metadata.RequestType.Name} => {metadata.HandlerClass.Name}.{metadata.HandlerMethod.Name}");

                }

                
                metadataList.Add(metadata);
            }

            return metadataList;
        }
    }

    public class RpcHandlerMetadata
    {
        public Type HandlerClass { get; set; }

        public MethodInfo HandlerMethod { get; set; }
        
        public Type RequestType { get; set; }
        
        public Type ResponseType { get; set; }
        
        public List<Type> Parameters { get; set; }
        
        public Dictionary<Type, RpcSupplementalAttribute> SupplementalAttributes { get; set; }
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
