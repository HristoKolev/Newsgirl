namespace PocoCodeGenerator
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Text.Json;
    using System.Threading.Tasks;
    using Humanizer;
    using Npgsql;
    using NpgsqlTypes;
    using RenderRazor;

    public static class Program
    {
        public static async Task<int> Main()
        {
            string settingsPath = GetLocalPath("settings.json");

            var settings = JsonSerializer.Deserialize<Settings>(await File.ReadAllBytesAsync(settingsPath));
            
            string getRelationsSql = await File.ReadAllTextAsync(GetLocalPath("get-relations.sql"));

            string getFunctionsSql = await File.ReadAllTextAsync(GetLocalPath("get-functions.sql"));

            await using (var connection = new NpgsqlConnection(settings!.ConnectionString))
            {
                var schemaRepository = new DbSchemaRepository(connection);

                var tables = await schemaRepository.GetTables(getRelationsSql);

                var functions = await schemaRepository.GetFunctions(getFunctionsSql);

                var context = new PocoTemplateContext
                {
                    Tables = tables,
                    Functions = functions,
                    Namespace = settings.Namespace,
                    PocoClassName = "DbPocos",
                    MetadataClassName = "DbMetadata",
                };

                string templateString = await File.ReadAllTextAsync(GetLocalPath("relations-template.txt"));

                var render = new RazorRenderer<PocoTemplateContext>(templateString, new[]
                {
                    typeof(Enumerable).Assembly,
                });

                render.Compile();

                string contents = await render.Render(context);

                await File.WriteAllTextAsync(settings.OutputPath, contents);
            }

            return 0;
        }

        private static string GetLocalPath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, $"../../../{path}");
        }
    }

    public class Settings
    {
        public string ConnectionString { get; set; }
        
        public string Namespace { get; set; }

        public string OutputPath { get; set; }
    }

    public class PocoTemplateContext
    {
        public List<TableMetadataModel> Tables { get; set; }

        public string Namespace { get; set; }

        public string PocoClassName { get; set; }

        public string MetadataClassName { get; set; }

        public List<FunctionMetadataModel> Functions { get; set; }
    }

    public static class CodeGeneratorHelper
    {
        private static Dictionary<string, string> ClrTypesByDatabaseTypes { get; }

        private static Dictionary<string, string> Linq2DbDataTypesByDbDataTypes { get; }

        private static Dictionary<string, NpgsqlDbType> NpgsTypesByDatabaseTypes { get; }

        private static Dictionary<string, string> PostgreSqlTypeAliases { get; }

        private static List<string> TypesThatCanBeNullable { get; }

        static CodeGeneratorHelper()
        {
            ClrTypesByDatabaseTypes = new Dictionary<string, string>
            {
                {"smallint", "short"},
                {"integer", "int"},
                {"bigint", "long"},
                {"real", "float"},
                {"double precision", "double"},
                {"boolean", "bool"},
                {"text", "string"},
                {"xml", "string"},
                {"date", "DateTime"},
                {"bytea", "byte[]"},
                {"uuid", "string"},
                {"json", "string"},
                {"jsonb", "string"},
                {"character varying", "string"},
                {"character", "string"},
                {"numeric", "decimal"},
                {"timestamp with time zone", "DateTimeOffset"},
                {"timestamp without time zone", "DateTime"},

                {"_int8", "long[]"},
                {"bigint[]", "long[]"},
            };

            Linq2DbDataTypesByDbDataTypes = new Dictionary<string, string>
            {
                {"character", "DataType.NChar"},
                {"text", "DataType.Text"},
                {"smallint", "DataType.Int16"},
                {"integer", "DataType.Int32"},
                {"bigint", "DataType.Int64"},
                {"real", "DataType.Single"},
                {"double precision", "DataType.Double"},
                {"bytea", "DataType.Binary"},
                {"boolean", "DataType.Boolean"},
                {"numeric", "DataType.Decimal"},
                {"money", "DataType.Money"},
                {"uuid", "DataType.Guid"},
                {"character varying", "DataType.NVarChar"},
                {"timestamp with time zone", "DataType.DateTimeOffset"},
                {"timestamp without time zone", "DataType.DateTime2"},
                {"time with time zone", "DataType.Time"},
                {"time without time zone", "DataType.Time"},
                {"interval", "DataType.Time"},
                {"date", "DataType.Date"},
                {"xml", "DataType.Xml"},
                {"point", "DataType.Udt"},
                {"lseg", "DataType.Udt"},
                {"box", "DataType.Udt"},
                {"circle", "DataType.Udt"},
                {"path", "DataType.Udt"},
                {"line", "DataType.Udt"},
                {"polygon", "DataType.Udt"},
                {"macaddr", "DataType.Udt"},
                {"USER-DEFINED", "DataType.Udt"},
                {"bit", "DataType.BitArray"},
                {"bit varying", "DataType.BitArray"},
                {"hstore", "DataType.Dictionary"},
                {"json", "DataType.Json"},
                {"jsonb", "DataType.BinaryJson"},

                {"_int8", "DataType.Undefined"},
                {"bigint[]", "DataType.Undefined"},
            };

            NpgsTypesByDatabaseTypes = new Dictionary<string, NpgsqlDbType>
            {
                {"bigint", NpgsqlDbType.Bigint},
                {"boolean", NpgsqlDbType.Boolean},
                {"box", NpgsqlDbType.Box},
                {"bytea", NpgsqlDbType.Bytea},
                {"circle", NpgsqlDbType.Circle},
                {"bpchar", NpgsqlDbType.Char},
                {"date", NpgsqlDbType.Date},
                {"double precision", NpgsqlDbType.Double},
                {"integer", NpgsqlDbType.Integer},
                {"line", NpgsqlDbType.Line},
                {"lseg", NpgsqlDbType.LSeg},
                {"money", NpgsqlDbType.Money},
                {"numeric", NpgsqlDbType.Numeric},
                {"path", NpgsqlDbType.Path},
                {"point", NpgsqlDbType.Point},
                {"polygon", NpgsqlDbType.Polygon},
                {"real", NpgsqlDbType.Real},
                {"smallint", NpgsqlDbType.Smallint},
                {"text", NpgsqlDbType.Text},
                {"time without time zone", NpgsqlDbType.Time},
                {"timestamp without time zone", NpgsqlDbType.Timestamp},
                {"character varying", NpgsqlDbType.Varchar},
                {"refcursor", NpgsqlDbType.Refcursor},
                {"inet", NpgsqlDbType.Inet},
                {"bit", NpgsqlDbType.Bit},
                {"uuid", NpgsqlDbType.Uuid},
                {"xml", NpgsqlDbType.Xml},
                {"oidvector", NpgsqlDbType.Oidvector},
                {"interval", NpgsqlDbType.Interval},
                {"time with time zone", NpgsqlDbType.TimeTz},
                {"timestamp with time zone", NpgsqlDbType.TimestampTz},
                {"name", NpgsqlDbType.Name},
                {"macaddr", NpgsqlDbType.MacAddr},
                {"json", NpgsqlDbType.Json},
                {"jsonb", NpgsqlDbType.Jsonb},
                {"character", NpgsqlDbType.Char},
                {"bit varying", NpgsqlDbType.Varbit},
                {"unknown", NpgsqlDbType.Unknown},
                {"oid", NpgsqlDbType.Oid},
                {"xid", NpgsqlDbType.Xid},
                {"cid", NpgsqlDbType.Cid},
                {"cidr", NpgsqlDbType.Cidr},
                {"tsvector", NpgsqlDbType.TsVector},
                {"tsquery", NpgsqlDbType.TsQuery},
                {"regtype", NpgsqlDbType.Regtype},
                {"int2vector", NpgsqlDbType.Int2Vector},
                {"tid", NpgsqlDbType.Tid},
                {"macaddr8", NpgsqlDbType.MacAddr8},

                {"_int8", NpgsqlDbType.Bigint | NpgsqlDbType.Array},
                {"bigint[]", NpgsqlDbType.Bigint | NpgsqlDbType.Array},
            };

            TypesThatCanBeNullable = new List<string>
            {
                "short",
                "int",
                "long",
                "char",

                "bool",

                "double",
                "float",
                "decimal",

                "DateTime",
                "DateTimeOffset",
            };

            PostgreSqlTypeAliases = new Dictionary<string, string>
            {
                {"int8", "bigint"},
                {"serial8", "bigserial"},
                {"varbit", "bit varying"},
                {"bool", "boolean"},
                {"char", "character"},
                {"varchar", "character varying"},
                {"float8", "double precision"},
                {"int", "integer"},
                {"int4", "integer"},
                {"decimal", "numeric"},
                {"float4", "real"},
                {"int2", "smallint"},
                {"serial2", "smallserial"},
                {"serial4", "serial"},
                {"timetz", "time with time zone"},
                {"timestamptz", "timestamp with time zone"},
            };

            ApplyAliases(ClrTypesByDatabaseTypes);
            ApplyAliases(Linq2DbDataTypesByDbDataTypes);
            ApplyAliases(NpgsTypesByDatabaseTypes);
        }

        private static void ApplyAliases<T>(Dictionary<string, T> dictionary)
        {
            foreach (var alias in PostgreSqlTypeAliases)
            {
                if (dictionary.ContainsKey(alias.Value))
                {
                    var value = dictionary[alias.Value];

                    dictionary.Add(alias.Key, value);
                }
            }
        }

        public static string GetClassName(string tableName)
        {
            var parts = tableName.Split('_')
                .Select(Singularize)
                .Select(x => char.ToUpper(x.First()) + x.Substring(1).ToLower());

            return string.Join(string.Empty, parts);
        }

        public static string GetLinq2DbDataType(string dbDataType)
        {
            return Linq2DbDataTypesByDbDataTypes[dbDataType];
        }

        public static NpgsqlDbType GetNpgsqlDbType(string dbDataType)
        {
            return NpgsTypesByDatabaseTypes[dbDataType];
        }

        public static string GetPluralClassName(string tableName)
        {
            var parts = tableName.Split('_').Select(x => char.ToUpper(x.First()) + x.Substring(1).ToLower());

            return string.Join(string.Empty, parts);
        }

        public static string GetPropertyName(string name)
        {
            var parts = name.Split('_').Select(x =>
            {
                if (x.ToLower() == "id")
                {
                    return "ID";
                }

                return char.ToUpper(x.First()) + x.Substring(1).ToLower();
            });

            return string.Join(string.Empty, parts);
        }

        public static string GetClrType(string dbDataType, bool isNullable)
        {
            string clrType = ClrTypesByDatabaseTypes[dbDataType];

            if (isNullable && TypesThatCanBeNullable.Contains(clrType))
            {
                clrType += "?";
            }

            return clrType;
        }

        public static string GetNullablePropertyType(string dbDataType)
        {
            string clrType = ClrTypesByDatabaseTypes[dbDataType];

            if (TypesThatCanBeNullable.Contains(clrType))
            {
                clrType += "?";
            }

            return clrType;
        }

        private static string Singularize(string word)
        {
            return word.Singularize(false);
        }
    }

    public static class TypeExtensions
    {
        public static string GetNpgsqlDbTypeLiteral(this NpgsqlDbType obj)
        {
            if ((int) obj < 0)
            {
                obj -= NpgsqlDbType.Array;
                return $"NpgsqlDbType.{obj.ToString()} | NpgsqlDbType.Array";
            }

            if ((int) obj > 1000)
            {
                obj -= NpgsqlDbType.Range;
                return $"NpgsqlDbType.{obj.ToString()} | NpgsqlDbType.Range";
            }

            return $"NpgsqlDbType.{obj.ToString()}";
        }
    }

    public class DbSchemaRepository
    {
        private NpgsqlConnection Connection { get; }

        public DbSchemaRepository(NpgsqlConnection connection)
        {
            this.Connection = connection;
        }

        public async Task<List<TableMetadataModel>> GetTables(string sql)
        {
            var columns = await Execute<ColumnMetadataModel>(this.Connection, sql);

            var tables = columns.GroupBy(model => model.TableName)
                .Select(group => new TableMetadataModel
                {
                    TableName = group.Key,
                    Columns = group.ToList(),
                })
                .OrderBy(x => x.IsView)
                .ThenBy(t => t.TableName)
                .ToList();

            foreach (var table in tables)
            {
                table.ClassName = CodeGeneratorHelper.GetClassName(table.TableName);
                table.PluralClassName = CodeGeneratorHelper.GetPluralClassName(table.TableName);
                table.TableSchema = table.Columns.First().TableSchema;
                table.IsView = table.Columns.Any(x => x.IsViewColumn);

                foreach (var column in table.Columns)
                {
                    column.PropertyType = new SimpleType(column.DbDataType, column.IsNullable);
                    column.PropertyName = CodeGeneratorHelper.GetPropertyName(column.ColumnName);
                    column.IsPrimaryKey = column.PrimaryKeyConstraintName != null;
                    column.IsForeignKey = column.ForeignKeyConstraintName != null;
                    column.Comments = (column.ColumnComment ?? string.Empty)
                        .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
                }

                if (!table.IsView)
                {
                    var pkColumn = table.Columns.Single(c => c.IsPrimaryKey);

                    table.PrimaryKeyColumnName = pkColumn.ColumnName;
                    table.PrimaryKeyPropertyName = CodeGeneratorHelper.GetPropertyName(pkColumn.ColumnName);
                }
            }

            return tables.ToList();
        }

        public async Task<List<FunctionMetadataModel>> GetFunctions(string sql)
        {
            var functions = await Execute<FunctionMetadataModel>(this.Connection, sql);

            foreach (var function in functions)
            {
                function.FunctionReturnType = new SimpleType(function.FunctionReturnTypeName, true);
                function.MethodName = CodeGeneratorHelper.GetPropertyName(function.FunctionName);
                function.Comments = (function.FunctionComment ?? string.Empty)
                    .Split(new[] {"\r\n"}, StringSplitOptions.RemoveEmptyEntries);
            }

            return functions;
        }
        
        private static readonly ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>> PropertiesCache =
            new ConcurrentDictionary<Type, Dictionary<string, PropertyInfo>>();

        private static async Task<List<T>> Execute<T>(NpgsqlConnection connection, string sql, params NpgsqlParameter[] parameters) where T : new()
        {
            if (connection.State == ConnectionState.Closed)
            {
                await connection.OpenAsync();
            }

            await using (var command = connection.CreateCommand())
            {
                command.Parameters.AddRange(parameters);
                command.CommandText = sql;

                var properties = PropertiesCache.GetOrAdd(typeof(T), t => t.GetProperties().ToDictionary(x => x.Name.ToLower(), x => x)
                );

                await using (var reader = await command.ExecuteReaderAsync())
                {
                    var list = new List<T>();

                    while (await reader.ReadAsync())
                    {
                        var instance = new T();

                        for (int i = 0; i < reader.FieldCount; i++)
                        {
                            var property = properties[reader.GetName(i)];

                            if (await reader.IsDBNullAsync(i))
                            {
                                property.SetValue(instance, null);
                            }
                            else
                            {
                                property.SetValue(instance, reader.GetValue(i));
                            }
                        }

                        list.Add(instance);
                    }

                    return list;
                }
            }
        }
    }

    /// <summary>
    /// Represents a table in PostgreSQL
    /// </summary>
    public class TableMetadataModel
    {
        public string ClassName { get; set; }

        public List<ColumnMetadataModel> Columns { get; set; }

        public string PluralClassName { get; set; }

        public string PrimaryKeyColumnName { get; set; }

        public string TableName { get; set; }

        public string TableSchema { get; set; }

        public string PrimaryKeyPropertyName { get; set; }

        public bool IsView { get; set; }
    }

    /// <summary>
    /// Represents a column in PostgreSQL
    /// </summary>
    public class ColumnMetadataModel
    {
        public string ColumnComment { get; set; }

        public string ColumnName { get; set; }

        public string[] Comments { get; set; }

        public bool IsPrimaryKey { get; set; }

        public string PrimaryKeyConstraintName { get; set; }

        public string ForeignKeyConstraintName { get; set; }

        public string TableName { get; set; }

        public string TableSchema { get; set; }

        public string DbDataType { get; set; }

        public bool IsNullable { get; set; }

        public string PropertyName { get; set; }

        public string ForeignKeyReferenceTableName { get; set; }

        public string ForeignKeyReferenceColumnName { get; set; }

        public string ForeignKeyReferenceSchemaName { get; set; }

        public bool IsForeignKey { get; set; }

        public bool IsViewColumn { get; set; }

        public SimpleType PropertyType { get; set; }
    }

    public class FunctionMetadataModel
    {
        public string SchemaName { get; set; }

        public string FunctionName { get; set; }

        public string MethodName { get; set; }

        public string FunctionDefinition { get; set; }

        public string FunctionReturnTypeName { get; set; }

        public SimpleType FunctionReturnType { get; set; }

        public string FunctionComment { get; set; }

        public string FunctionArgumentsAsString { get; set; }

        public Dictionary<string, SimpleType> FunctionArguments
        {
            get
            {
                return this.FunctionArgumentsAsString.Split(',', StringSplitOptions.RemoveEmptyEntries).ToDictionary(
                    x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(),
                    x => new SimpleType(string.Join(" ", x.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)), true));
            }
        }

        public string[] Comments { get; set; }
    }

    public class SimpleType
    {
        public SimpleType(string dbDataType, bool isNullable)
        {
            this.DbDataType = dbDataType;
            this.IsNullable = isNullable;
            this.ClrTypeName = CodeGeneratorHelper.GetClrType(dbDataType, isNullable);
            this.ClrNonNullableTypeName = this.ClrTypeName.Trim('?');
            this.ClrNullableTypeName = CodeGeneratorHelper.GetNullablePropertyType(dbDataType);
            this.Linq2DbDataTypeName = CodeGeneratorHelper.GetLinq2DbDataType(dbDataType);
            this.NpgsqlDbTypeName = CodeGeneratorHelper.GetNpgsqlDbType(dbDataType).GetNpgsqlDbTypeLiteral();

            this.IsClrValueType = this.ClrTypeName != "string";
            this.IsClrNullableType = this.ClrTypeName != "string" && isNullable;
            this.IsClrReferenceType = this.ClrTypeName == "string" || isNullable;
        }

        public string Linq2DbDataTypeName { get; set; }

        public string ClrTypeName { get; set; }

        public string DbDataType { get; set; }

        public bool IsNullable { get; set; }

        public string NpgsqlDbTypeName { get; set; }

        public string ClrNonNullableTypeName { get; set; }

        public string ClrNullableTypeName { get; set; }

        public bool IsClrValueType { get; set; }

        public bool IsClrNullableType { get; set; }

        public bool IsClrReferenceType { get; set; }
    }
}
