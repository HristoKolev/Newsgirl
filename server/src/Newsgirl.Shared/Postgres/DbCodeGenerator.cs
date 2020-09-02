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
        /// <summary>
        /// Cache dictionary for objects generated with the `GenerateSetters` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> GenerateSettersCache = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Cache dictionary for objects generated with the `GenerateGetters` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> GenerateGettersCache = new ConcurrentDictionary<Type, object>();

        /// <summary>
        /// Cache dictionary for objects generated with the `GetMetadata` method.
        /// </summary>
        private static readonly ConcurrentDictionary<Type, object> GetMetadataCache = new ConcurrentDictionary<Type, object>();

        private static bool IsNullableType(Type type)
        {
            return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
        }

        private static Func<T, object> GetGetter<T>(string propertyName)
        {
            var instanceType = typeof(T);

            var property = instanceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (!property.IsAutoImplemented())
            {
                throw new ApplicationException($"The property `{property.Name}` of type `{instanceType.Name}` is not auto implemented.");
            }

            return arg => property.GetBackingField().GetValue(arg);
        }

        private static Action<T, object> GetSetter<T>(string propertyName)
        {
            var instanceType = typeof(T);

            var property = instanceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            if (!property.IsAutoImplemented())
            {
                throw new ApplicationException($"The property `{property.Name}` of type `{instanceType.Name}` is not auto implemented.");
            }

            return (obj, value) => property.GetBackingField().SetValue(obj, value);
        }

        public static Func<T, T> GetClone<T>() where T : new()
        {
            var instanceType = typeof(T);

            return obj =>
            {
                var instance = new T();

                foreach (var fieldInfo in instanceType.GetFields(
                    BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
                {
                    fieldInfo.SetValue(instance, fieldInfo.GetValue(obj));
                }

                return instance;
            };
        }

        public static Func<TPoco, NpgsqlParameter[]> GetGenerateParameters<TPoco>(TableMetadataModel<TPoco> metadata)
            where TPoco : IPoco<TPoco>
        {
            var pocoType = typeof(TPoco);

            var nonPrimaryKeyColumns = metadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();

            return poco =>
            {
                var list = new List<NpgsqlParameter>();

                foreach (var column in nonPrimaryKeyColumns)
                {
                    var property = pocoType.GetProperty(column.PropertyName);

                    list.Add(new NpgsqlParameter
                    {
                        Value = property.GetValue(poco) ?? DBNull.Value,
                        NpgsqlDbType = column.PropertyType.NpgsqlDbType,
                    });
                }

                return list.ToArray();
            };
        }

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

                    var value = property.GetValue(poco);

                    if (value == null)
                    {
                        importer.WriteNull();
                    }
                    else
                    {
                        var type = IsNullableType(property.PropertyType)
                            ? Nullable.GetUnderlyingType(property.PropertyType)
                            : property.PropertyType;

                        genericWrite.MakeGenericMethod(type)
                            .Invoke(importer, new[]
                            {
                                value, column.PropertyType.NpgsqlDbType,
                            });
                    }
                }
            };
        }

        public static Func<TPoco, ValueTuple<string[], NpgsqlParameter[]>> GetGetAllColumns<TPoco>(TableMetadataModel<TPoco> metadata)
            where TPoco : IPoco<TPoco>
        {
            var pocoType = typeof(TPoco);

            var nonPrimaryKeyColumns = metadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();

            return poco =>
            {
                var names = new List<string>();
                var list = new List<NpgsqlParameter>();

                foreach (var column in nonPrimaryKeyColumns)
                {
                    var property = pocoType.GetProperty(column.PropertyName);

                    names.Add(column.ColumnName);

                    list.Add(new NpgsqlParameter
                    {
                        Value = property.GetValue(poco) ?? DBNull.Value,
                        NpgsqlDbType = column.PropertyType.NpgsqlDbType,
                    });
                }

                return (names.ToArray(), list.ToArray());
            };
        }

        public static Func<TPoco, TPoco, ValueTuple<List<string>, List<NpgsqlParameter>>> GetGetColumnChanges<TPoco>(TableMetadataModel<TPoco> metadata)
            where TPoco : IPoco<TPoco>
        {
            var pocoType = typeof(TPoco);

            var nonPrimaryKeyColumns = metadata.Columns.Where(x => !x.IsPrimaryKey).ToArray();

            return (obj1, obj2) =>
            {
                var names = new List<string>();
                var parameters = new List<NpgsqlParameter>();

                foreach (var column in nonPrimaryKeyColumns)
                {
                    var property = pocoType.GetProperty(column.PropertyName);

                    var value1 = property.GetValue(obj1);
                    var value2 = property.GetValue(obj2);

                    if (!StupidEquals(value1, value2))
                    {
                        names.Add(column.ColumnName);
                        parameters.Add(new NpgsqlParameter
                        {
                            Value = property.GetValue(obj2) ?? DBNull.Value,
                            NpgsqlDbType = column.PropertyType.NpgsqlDbType,
                        });
                    }
                }

                return (names, parameters);
            };
        }

        public static Dictionary<string, Action<T, object>> GenerateSetters<T>(Func<string, string> propertyNameToColumnName)
        {
            Dictionary<string, Action<T, object>> ValueFactory(Type type)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(x => x.SetMethod != null)
                    .ToDictionary(x => propertyNameToColumnName(x.Name), x => GetSetter<T>(x.Name));
            }

            return (Dictionary<string, Action<T, object>>) GenerateGettersCache.GetOrAdd(typeof(T), ValueFactory);
        }

        public static Dictionary<string, Func<T, object>> GenerateGetters<T>(Func<string, string> propertyNameToColumnName)
        {
            Dictionary<string, Func<T, object>> ValueFactory(Type type)
            {
                return type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .ToDictionary(x => propertyNameToColumnName(x.Name), x => GetGetter<T>(x.Name));
            }

            return (Dictionary<string, Func<T, object>>) GenerateSettersCache.GetOrAdd(typeof(T), ValueFactory);
        }

        public static Dictionary<string, Action<T, object>> GenerateSetters<T>()
        {
            return GenerateSetters<T>(DefaultPropertyNameToColumnName);
        }

        public static Dictionary<string, Func<T, object>> GenerateGetters<T>()
        {
            return GenerateGetters<T>(DefaultPropertyNameToColumnName);
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
        private static string DefaultPropertyNameToColumnName(string propertyName)
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

        public static TableMetadataModel<TPoco> GetMetadata<TPoco>() where TPoco : IReadOnlyPoco<TPoco>
        {
            object ValueFactory(Type type)
            {
                var metadataProperty = type.GetProperty("Metadata", BindingFlags.Public | BindingFlags.Static);

                // ReSharper disable once PossibleNullReferenceException
                return metadataProperty.GetValue(null);
            }

            return (TableMetadataModel<TPoco>) GetMetadataCache.GetOrAdd(typeof(TPoco), ValueFactory);
        }

        private static bool StupidEquals(object a, object b)
        {
            return a != null && (a.GetType().IsValueType || a is string) ? Equals(a, b) : ReferenceEquals(a, b);
        }
    }

    public static class PropertyInfoExtensions
    {
        public static FieldInfo GetBackingField(this PropertyInfo prop)
        {
            return prop?.DeclaringType?.GetField($"<{prop.Name}>k__BackingField", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        public static bool IsAutoImplemented(this PropertyInfo prop)
        {
            return prop.GetBackingField() != null;
        }
    }
}
