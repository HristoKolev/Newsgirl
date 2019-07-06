namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Threading.Tasks;

    using CommandLine;

    [CliCommand("generate-client-rpc", SkipSettingsLoading = true)]
    // ReSharper disable once UnusedMember.Global
    public class RpcCodeGeneratorCommand : ICliCommand
    {
        public async Task<int> Run(string[] args)
        {
            var options = RpcCodeGeneratorOptions.ReadFromArgs(args);

            if (options == default)
            {
                return 1;
            }
            
            var handlers = Global.Handlers.GetAllHandlers().ToList();

            var types = handlers.Select(x => x.RequestType)
                                .Concat(handlers.Select(x => x.ResponseType))
                                .Where(x => x != null)
                                .ToList();

            var allTypes = GetAllTypes(types);

            string contents = string.Join("\n\n", allTypes.Select(ScriptType));

            await File.WriteAllTextAsync(options.Output, contents);
            
            return 0;
        }

        private static string ScriptType(Type targetType)
        {
            var props = targetType.GetProperties(BindingFlags.Public | BindingFlags.Instance);

            string ScriptProperty(PropertyInfo x)
            {
                bool isNullableType = x.PropertyType.IsGenericType &&
                                      x.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                
                return $"  {CamelCase(x.Name)}{(isNullableType ? "?" : "")}: {ResolveType(x.PropertyType)};";
            }

            var scriptedProperties = props.Select(ScriptProperty).ToList();

            return $"export interface {targetType.Name} {{\n" + string.Join("\n", scriptedProperties) + "\n}";
        }

        private static string ResolveType(Type type)
        {
            var typeMap = new Dictionary<Type, string>
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

            // If it's not a simple type - return it's name.
            if (!type.IsGenericType)
            {
                return type.Name;
            }

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

            if (genericDefinition == typeof(Nullable<>))
            {
                var genericArguments = type.GetGenericArguments();
                
                var t = genericArguments[0];

                return ResolveType(t);
            }
            
            throw new DetailedLogException("Generic type not supported.")
            {
                Context =
                {
                    {"TypeName", genericDefinition.Name},
                }
            };
        }

        private static string CamelCase(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return text;
            }

            if (text == "ID")
            {
                return "id";
            }
		 
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

                    var allowedGenericTypes = new[]
                    {
                        typeof(List<>),
                        typeof(Dictionary<,>),
                        typeof(Nullable<>),
                    };
                    
                    if (!allowedGenericTypes.Contains(genericTypeDefinition))
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
    
    // ReSharper disable once ClassNeverInstantiated.Global
    public class RpcCodeGeneratorOptions
    {
        [Option('o', "output", HelpText = "The output file location.", Required = true)]
        public string Output { get; set; }
 
        public static RpcCodeGeneratorOptions ReadFromArgs(string[] args)
        {
            RpcCodeGeneratorOptions cliArgs = default;

            Parser.Default.ParseArguments<RpcCodeGeneratorOptions>(args)
                  .WithParsed(x => cliArgs = x);

            return cliArgs;
        }
    }
}