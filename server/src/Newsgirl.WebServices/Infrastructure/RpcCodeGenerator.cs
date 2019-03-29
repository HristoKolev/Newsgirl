namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text.RegularExpressions;
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

            string contents = string.Join("\n\n", allTypes.Select(ScriptType));
            
            MainLogger.Instance.LogDebug(contents);
            
            return 0;
        }

        private static string ScriptType(Type targetType)
        {
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            var scriptedProperties = props.Select(x => $"  {CamelCase(x.Name)}: {ResolveType(x.PropertyType)};").ToList();

            return $"export interface {targetType.Name} {{\n" + string.Join("\n", scriptedProperties) + "\n}";
        }

        private static string ResolveType(Type type)
        {
            var typeMap = new Dictionary<Type, string>()
            {
                { typeof(int), "number" },
                { typeof(long), "number" },
                { typeof(string), "string" },
                { typeof(bool), "boolean" },
                { typeof(decimal), "number" },
                { typeof(DateTime), "Date" },
            };

            if (typeMap.ContainsKey(type))
            {
                return typeMap[type];
            }

            if (type.IsArray)
            {
                if (type.GetArrayRank() != 1)
                {
                    throw new DetailedLogException("Multidimensional arrays are not supported.");
                }
                
                var t = type.GetElementType();
                
                return ResolveType(t) + "[]";
            }

            if (type.IsGenericType)
            {
                var genericDefinition = type.GetGenericTypeDefinition();

                if (genericDefinition == typeof(List<>))
                {
                    var genericArguments = type.GetGenericArguments();
                    var t = genericArguments[0];

                    return ResolveType(t) + "[]";
                }

                if (genericDefinition == typeof(Dictionary<,>))
                {
                    var genericArguments = type.GetGenericArguments();
                    
                    var tKey = genericArguments[0];
                    var tValue = genericArguments[1];

                    return "{ [key: " + ResolveType(tKey) + "]: " + ResolveType(tValue) + " }";
                }
                
                throw new DetailedLogException("Generic type not supported.")
                {
                    Context =
                    {
                        {"TypeName", genericDefinition.Name},
                    }
                };
            }

            // If it's not a simple type - return it's name.
            return type.Name;
        }

        private static string CamelCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }
		
            text = Regex.Replace(text, "([A-Z])([A-Z]+)($|[A-Z])", m => m.Groups[1].Value + m.Groups[2].Value.ToLower() + m.Groups[3].Value);
		
            return char.ToLower(text[0]) + text.Substring(1);
        }
        
        private static List<Type> GetAllTypes(List<Type> types)
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

                if (targetType.IsArray)
                {
                    if (targetType.GetArrayRank() != 1)
                    {
                        throw new DetailedLogException("Multidimensional arrays are not supported.");
                    }
                
                    var t = targetType.GetElementType();
                    
                    Recurse(t);
                    return;
                }

                if (targetType.IsGenericType)
                {
                    var genericTypeDefinition = targetType.GetGenericTypeDefinition();

                    if (genericTypeDefinition != typeof(List<>) && genericTypeDefinition != typeof(Dictionary<,>))
                    {
                        throw new DetailedLogException("Generic type not supported.")
                        {
                            Context =
                            {
                                {"TypeName", targetType},
                            }
                        };
                    }
                    
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

            return allTypes.ToList();
        }
    }
}