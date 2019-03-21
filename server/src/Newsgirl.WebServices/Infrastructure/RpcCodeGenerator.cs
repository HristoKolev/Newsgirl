namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    public class RpcCodeGenerator
    {
        public static async Task<int> Generate(string[] args)
        {
            var handlers = Global.Handlers.GetAllHandlers().ToList();

            var types = handlers.Select(x => x.RequestType)
                                .Concat(handlers.Select(x => x.ResponseType))
                                .Where(x => x != null)
                                .ToList();

            var simpleTypes = new[]
            {
                typeof(int),
                typeof(long),
                typeof(decimal),
                typeof(string),
            };

            var allTypes = new HashSet<Type>();

            void Recurse(Type targetType)
            {
                if (allTypes.Add(targetType))
                {
                    var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

                    foreach (var prop in props)
                    {
                        Recurse(prop.PropertyType);
                    }
                }
            }

            foreach (var type in types)
            {
                Recurse(type);
            }

            foreach (Type type in allTypes)
            {
                MainLogger.Instance.LogDebug($"TYPE: {type.Name}");
            }

            return 0;
        }
    }
}