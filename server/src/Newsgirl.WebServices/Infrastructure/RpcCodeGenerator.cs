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

            var allTypes = GetAllTypes(types);

            foreach (Type type in allTypes)
            {
                MainLogger.Instance.LogDebug($"TYPE: {type.Name}");
            }

            return 0;
        }

        private static HashSet<Type> GetAllTypes(List<Type> types)
        {
            var bannedTypes = new[]
            {
                typeof(byte),
                typeof(sbyte),
                typeof(short),
                typeof(ushort),
                typeof(uint),
                typeof(ulong),
                typeof(float),
                typeof(char),
                typeof(double),
            };

            var simpleTypes = new[]
            {
                typeof(int),
                typeof(long),
                typeof(string),
                typeof(bool),
                typeof(decimal),
                typeof(DateTime)
            };

            var allTypes = new HashSet<Type>();

            void Recurse(Type targetType)
            {
                if (bannedTypes.Contains(targetType))
                {
                    throw new DetailedLogException("Banned type detected in one of the DTO classes.")
                    {
                        Context =
                        {
                            {"TypeName", targetType.Name},
                        }
                    };
                }

                if (simpleTypes.Contains(targetType))
                {
                    return;
                }

                if (targetType.IsGenericType)
                {
                    foreach (var t in targetType.GetGenericArguments())
                    {
                        Recurse(t);
                    }

                    return;
                }

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

            return allTypes;
        }
    }
}