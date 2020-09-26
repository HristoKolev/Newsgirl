namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using Npgsql;
    using NpgsqlTypes;

    public static class DbCodeGenerator
    {
        public static Action<NpgsqlBinaryImporter, TPoco> GetWriteToImporter<TPoco>(TableMetadataModel<TPoco> metadata)
            where TPoco : IPoco<TPoco>
        {
            var pocoType = typeof(TPoco);

            var genericWrite = typeof(NpgsqlBinaryImporter)
                .GetMethods()
                .Where((info, i) => info.GetParameters().Length == 2 && info.GetParameters()[1].ParameterType == typeof(NpgsqlDbType))
                .First();

            var nonPrimaryKeyColumns = metadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();

            return (importer, poco) =>
            {
                foreach (var column in nonPrimaryKeyColumns)
                {
                    var property = pocoType.GetProperty(column.PropertyName);

                    var value = property!.GetValue(poco);

                    if (value == null)
                    {
                        importer.WriteNull();
                    }
                    else
                    {
                        var type = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>)
                            ? Nullable.GetUnderlyingType(property.PropertyType)
                            : property.PropertyType;

                        genericWrite.MakeGenericMethod(type!).Invoke(importer, new[]
                        {
                            value, column.PropertyType.NpgsqlDbType,
                        });
                    }
                }
            };
        }

        /// <summary>
        /// Cache dictionary for objects generated with the `GenerateSetters` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> SettersCache = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Cache dictionary for objects generated with the `GenerateGetters` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> GettersCache = new ConcurrentDictionary<Type, object>();

        public static Dictionary<string, Action<T, object>> GenerateSetters<T>()
        {
            static Dictionary<string, Action<T, object>> ValueFactory(Type type)
            {
                var result = new Dictionary<string, Action<T, object>>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.SetMethod != null))
                {
                    Action<T, object> setter = (obj, value) => property!.SetMethod!.Invoke(obj, new[] {value});

                    result.TryAdd(property.Name, setter);
                    result.TryAdd(property.Name.Replace("_", ""), setter);
                    result.TryAdd(ConvertToSnakeCase(property.Name), setter);
                }

                return result;
            }

            return (Dictionary<string, Action<T, object>>) GettersCache.GetOrAdd(typeof(T), ValueFactory);
        }

        public static Dictionary<string, Func<T, object>> GenerateGetters<T>()
        {
            static Dictionary<string, Func<T, object>> ValueFactory(Type type)
            {
                var result = new Dictionary<string, Func<T, object>>();

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetMethod != null))
                {
                    Func<T, object> getter = arg => property!.GetMethod!.Invoke(arg, Array.Empty<object>());

                    result.TryAdd(property.Name, getter);
                    result.TryAdd(property.Name.Replace("_", ""), getter);
                    result.TryAdd(ConvertToSnakeCase(property.Name), getter);
                }

                return result;
            }

            return (Dictionary<string, Func<T, object>>) SettersCache.GetOrAdd(typeof(T), ValueFactory);
        }

        /// <summary>
        /// Converts `PascalCase` property names into `snake_case` column names.
        /// The conversion happens on Uppercase letter or the string `ID`.
        /// Examples:
        /// SystemSettingName => system_setting_name
        /// SystemSettingID => system_setting_id
        /// System_Setting_ID => system_setting_id
        /// system_setting_id => system_setting_id
        /// FK_Reference_Schema_Name => fk_reference_schema_name
        /// </summary>
        private static string ConvertToSnakeCase(string propertyName)
        {
            var sb = new StringBuilder();

            sb.Append(char.ToLower(propertyName[0]));

            for (int i = 1; i < propertyName.Length; i++)
            {
                bool NextIs(Func<char, bool> func)
                {
                    if (i + 1 >= propertyName.Length)
                    {
                        return false;
                    }

                    char nextChar = propertyName[i + 1];

                    return func(nextChar);
                }

                bool PrevIs(Func<char, bool> func)
                {
                    if (i - 1 < 0)
                    {
                        return false;
                    }

                    char prevChar = propertyName[i - 1];

                    return func(prevChar);
                }

                char c = propertyName[i];

                if (c == 'I' && NextIs(x => x == 'D'))
                {
                    sb.Append(PrevIs(x => x == '_') ? "id" : "_id");
                    i++;
                }
                else if (c == '_')
                {
                    sb.Append('_');
                }
                else if (char.IsUpper(c))
                {
                    if (!PrevIs(char.IsUpper) && !PrevIs(x => x == '_'))
                    {
                        sb.Append('_');
                    }

                    sb.Append(char.ToLower(c));
                }

                else
                {
                    sb.Append(c);
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Cache dictionary for objects generated with the `GetMetadata` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> MetadataCache = new ConcurrentDictionary<Type, object>();

        public static TableMetadataModel<TPoco> GetMetadata<TPoco>() where TPoco : IReadOnlyPoco<TPoco>
        {
            static object ValueFactory(Type type)
            {
                return type.GetProperty("Metadata", BindingFlags.Public | BindingFlags.Static)!
                    .GetValue(null);
            }

            return (TableMetadataModel<TPoco>) MetadataCache.GetOrAdd(typeof(TPoco), ValueFactory);
        }
    }
}
