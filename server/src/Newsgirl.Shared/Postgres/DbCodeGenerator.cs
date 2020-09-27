namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Linq;
    using System.Reflection;
    using System.Reflection.Emit;
    using System.Text;

    public static class DbCodeGenerator
    {
        private static readonly ConcurrentDictionary<Type, object> SettersCache = new ConcurrentDictionary<Type, object>();

        private static readonly ConcurrentDictionary<Type, object> GettersCache = new ConcurrentDictionary<Type, object>();

        private static readonly ConcurrentDictionary<Type, TableMetadataModel> MetadataCache = new ConcurrentDictionary<Type, TableMetadataModel>();

        public static Dictionary<string, Action<T, object>> GetSetters<T>()
        {
            static Dictionary<string, Action<T, object>> ValueFactory(Type type)
            {
                var result = new Dictionary<string, Action<T, object>>(StringComparer.OrdinalIgnoreCase);

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.SetMethod != null))
                {
                    var builder = new DynamicMethod($"{property.Name}_setter", typeof(void), new[] {typeof(T), typeof(object)});
                    var il = builder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Ldarg_1);
                    if (property.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Unbox_Any, property.PropertyType);
                    }

                    il.Emit(OpCodes.Call, property!.SetMethod!);
                    il.Emit(OpCodes.Ret);

                    var setter = builder.CreateDelegate<Action<T, object>>();

                    result.TryAdd(property.Name, setter);
                    result.TryAdd(property.Name.Replace("_", ""), setter);
                    result.TryAdd(ConvertToSnakeCase(property.Name), setter);
                }

                return result;
            }

            return (Dictionary<string, Action<T, object>>) GettersCache.GetOrAdd(typeof(T), ValueFactory);
        }

        public static Dictionary<string, Func<T, object>> GetGetters<T>()
        {
            static Dictionary<string, Func<T, object>> ValueFactory(Type type)
            {
                var result = new Dictionary<string, Func<T, object>>();

                foreach (var property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.GetMethod != null))
                {
                    var builder = new DynamicMethod($"{property.Name}_getter", typeof(object), new[] {typeof(T)});
                    var il = builder.GetILGenerator();
                    il.Emit(OpCodes.Ldarg_0);
                    il.Emit(OpCodes.Call, property!.GetMethod!);

                    if (property.PropertyType.IsValueType)
                    {
                        il.Emit(OpCodes.Box, property.PropertyType);
                    }

                    il.Emit(OpCodes.Ret);

                    var getter = builder.CreateDelegate<Func<T, object>>();

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

        public static TableMetadataModel GetMetadata<TPoco>() where TPoco : IReadOnlyPoco<TPoco>
        {
            static TableMetadataModel ValueFactory(Type type)
            {
                return (TableMetadataModel) type.GetProperty("Metadata", BindingFlags.Public | BindingFlags.Static)!
                    .GetValue(null);
            }

            return MetadataCache.GetOrAdd(typeof(TPoco), ValueFactory);
        }
    }
}
