namespace PocoCodeGenerator
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Linq;
    using System.Threading.Tasks;
    using Humanizer;
    using Newsgirl.Shared;
    using Newsgirl.Shared.Postgres;
    using Npgsql;
    using NpgsqlTypes;
    using RenderRazor;

    public static class Program
    {
        public static async Task Main()
        {
            string settingsPath = GetLocalPath("settings.json");

            var settings = JsonHelper.Deserialize<Setting[]>(await File.ReadAllBytesAsync(settingsPath));

            foreach (var setting in settings)
            {
                await using (var connection = new NpgsqlConnection(setting!.ConnectionString))
                {
                    var schemaRepository = new DbSchemaRepository(connection);

                    var tables = await schemaRepository.GetTables();

                    var functions = await schemaRepository.GetFunctions();

                    var context = new PocoTemplateContext
                    {
                        Tables = tables,
                        Functions = functions,
                        Namespace = setting.Namespace,
                        PocoClassName = setting.PocoClassName,
                        MetadataClassName = setting.MetadataClassName,
                    };

                    string templateString = await File.ReadAllTextAsync(GetLocalPath(setting.TemplatePath));

                    var render = new RazorRenderer<PocoTemplateContext>(templateString, new[]
                    {
                        typeof(Enumerable).Assembly,
                        typeof(SimpleType).Assembly,
                    });

                    render.Compile();

                    string contents = await render.Render(context);

                    await File.WriteAllTextAsync(setting.OutputPath, contents);
                }
            }
        }

        private static string GetLocalPath(string path)
        {
            return Path.Combine(Path.GetDirectoryName(typeof(Program).Assembly.Location)!, $"../../../{path}");
        }
    }

    public class Setting
    {
        public string ConnectionString { get; set; }

        public string Namespace { get; set; }

        public string PocoClassName { get; set; }

        public string OutputPath { get; set; }

        public string MetadataClassName { get; set; }

        public string TemplatePath { get; set; }
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
                { "smallint", "short" },
                { "integer", "int" },
                { "bigint", "long" },
                { "real", "float" },
                { "double precision", "double" },
                { "boolean", "bool" },
                { "text", "string" },
                { "xml", "string" },
                { "date", "DateTime" },
                { "bytea", "byte[]" },
                { "uuid", "string" },
                { "json", "string" },
                { "jsonb", "string" },
                { "character varying", "string" },
                { "character", "string" },
                { "numeric", "decimal" },
                { "timestamp with time zone", "DateTimeOffset" },
                { "timestamp without time zone", "DateTime" },
                { "_int8", "long[]" },
                { "bigint[]", "long[]" },
            };

            Linq2DbDataTypesByDbDataTypes = new Dictionary<string, string>
            {
                { "character", "DataType.NChar" },
                { "text", "DataType.Text" },
                { "smallint", "DataType.Int16" },
                { "integer", "DataType.Int32" },
                { "bigint", "DataType.Int64" },
                { "real", "DataType.Single" },
                { "double precision", "DataType.Double" },
                { "bytea", "DataType.Binary" },
                { "boolean", "DataType.Boolean" },
                { "numeric", "DataType.Decimal" },
                { "money", "DataType.Money" },
                { "uuid", "DataType.Guid" },
                { "character varying", "DataType.NVarChar" },
                { "timestamp with time zone", "DataType.DateTimeOffset" },
                { "timestamp without time zone", "DataType.DateTime2" },
                { "time with time zone", "DataType.Time" },
                { "time without time zone", "DataType.Time" },
                { "interval", "DataType.Time" },
                { "date", "DataType.Date" },
                { "xml", "DataType.Xml" },
                { "point", "DataType.Udt" },
                { "lseg", "DataType.Udt" },
                { "box", "DataType.Udt" },
                { "circle", "DataType.Udt" },
                { "path", "DataType.Udt" },
                { "line", "DataType.Udt" },
                { "polygon", "DataType.Udt" },
                { "macaddr", "DataType.Udt" },
                { "USER-DEFINED", "DataType.Udt" },
                { "bit", "DataType.BitArray" },
                { "bit varying", "DataType.BitArray" },
                { "hstore", "DataType.Dictionary" },
                { "json", "DataType.Json" },
                { "jsonb", "DataType.BinaryJson" },
                { "_int8", "DataType.Undefined" },
                { "bigint[]", "DataType.Undefined" },
            };

            NpgsTypesByDatabaseTypes = new Dictionary<string, NpgsqlDbType>
            {
                { "bigint", NpgsqlDbType.Bigint },
                { "boolean", NpgsqlDbType.Boolean },
                { "box", NpgsqlDbType.Box },
                { "bytea", NpgsqlDbType.Bytea },
                { "circle", NpgsqlDbType.Circle },
                { "bpchar", NpgsqlDbType.Char },
                { "date", NpgsqlDbType.Date },
                { "double precision", NpgsqlDbType.Double },
                { "integer", NpgsqlDbType.Integer },
                { "line", NpgsqlDbType.Line },
                { "lseg", NpgsqlDbType.LSeg },
                { "money", NpgsqlDbType.Money },
                { "numeric", NpgsqlDbType.Numeric },
                { "path", NpgsqlDbType.Path },
                { "point", NpgsqlDbType.Point },
                { "polygon", NpgsqlDbType.Polygon },
                { "real", NpgsqlDbType.Real },
                { "smallint", NpgsqlDbType.Smallint },
                { "text", NpgsqlDbType.Text },
                { "time without time zone", NpgsqlDbType.Time },
                { "timestamp without time zone", NpgsqlDbType.Timestamp },
                { "character varying", NpgsqlDbType.Varchar },
                { "refcursor", NpgsqlDbType.Refcursor },
                { "inet", NpgsqlDbType.Inet },
                { "bit", NpgsqlDbType.Bit },
                { "uuid", NpgsqlDbType.Uuid },
                { "xml", NpgsqlDbType.Xml },
                { "oidvector", NpgsqlDbType.Oidvector },
                { "interval", NpgsqlDbType.Interval },
                { "time with time zone", NpgsqlDbType.TimeTz },
                { "timestamp with time zone", NpgsqlDbType.TimestampTz },
                { "name", NpgsqlDbType.Name },
                { "macaddr", NpgsqlDbType.MacAddr },
                { "json", NpgsqlDbType.Json },
                { "jsonb", NpgsqlDbType.Jsonb },
                { "character", NpgsqlDbType.Char },
                { "bit varying", NpgsqlDbType.Varbit },
                { "unknown", NpgsqlDbType.Unknown },
                { "oid", NpgsqlDbType.Oid },
                { "xid", NpgsqlDbType.Xid },
                { "cid", NpgsqlDbType.Cid },
                { "cidr", NpgsqlDbType.Cidr },
                { "tsvector", NpgsqlDbType.TsVector },
                { "tsquery", NpgsqlDbType.TsQuery },
                { "regtype", NpgsqlDbType.Regtype },
                { "int2vector", NpgsqlDbType.Int2Vector },
                { "tid", NpgsqlDbType.Tid },
                { "macaddr8", NpgsqlDbType.MacAddr8 },
                { "_int8", NpgsqlDbType.Bigint | NpgsqlDbType.Array },
                { "bigint[]", NpgsqlDbType.Bigint | NpgsqlDbType.Array },
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
                { "int8", "bigint" },
                { "serial8", "bigserial" },
                { "varbit", "bit varying" },
                { "bool", "boolean" },
                { "char", "character" },
                { "varchar", "character varying" },
                { "float8", "double precision" },
                { "int", "integer" },
                { "int4", "integer" },
                { "decimal", "numeric" },
                { "float4", "real" },
                { "int2", "smallint" },
                { "serial2", "smallserial" },
                { "serial4", "serial" },
                { "timetz", "time with time zone" },
                { "timestamptz", "timestamp with time zone" },
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
                .Select(x => char.ToUpper(x.First()) + x[1..].ToLower());

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
            var parts = tableName.Split('_').Select(x => char.ToUpper(x.First()) + x[1..].ToLower());

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

                return char.ToUpper(x.First()) + x[1..].ToLower();
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
            if ((int)obj < 0)
            {
                obj -= NpgsqlDbType.Array;
                return $"NpgsqlDbType.{obj.ToString()} | NpgsqlDbType.Array";
            }

            if ((int)obj > 1000)
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

        public async Task<List<TableMetadataModel>> GetTables()
        {
            var columns = await this.Connection.Query<ColumnMetadataModel>(@"
              ((SELECT
                  t.tablename as TableName,
                  n.nspname AS TableSchema,
                  a.attname as ColumnName,
                  pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
                  pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
                  (a.attnotnull = FALSE) AS IsNullable,
                  p.conname AS PrimaryKeyConstraintName,
                  f.conname AS ForeignKeyConstraintName,
                  fc.relname as ForeignKeyReferenceTableName,
                  CASE WHEN pg_catalog.pg_get_constraintdef(f.oid) LIKE 'FOREIGN KEY %'
                          THEN substring(pg_catalog.pg_get_constraintdef(f.oid),
                              position('(' in substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14, position(')'
                              in substring(pg_catalog.pg_get_constraintdef(f.oid), position('(' in
                              substring(pg_catalog.pg_get_constraintdef(f.oid), 14))+14))-1) END AS ForeignKeyReferenceColumnName,
                  fn.nspname as ForeignKeyReferenceSchemaName,
                  FALSE as IsViewColumn

                  FROM pg_catalog.pg_class c
                  JOIN pg_catalog.pg_tables t ON c.relname = t.tablename
                  JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
                  JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace
                  LEFT JOIN pg_catalog.pg_constraint p ON p.contype = 'p'::char AND p.conrelid = c.oid AND (a.attnum = ANY (p.conkey))
                  LEFT JOIN pg_catalog.pg_constraint f ON f.contype = 'f'::char AND f.conrelid = c.oid AND (a.attnum = ANY (f.conkey))
                  LEFT JOIN pg_catalog.pg_class fc on fc.oid = f.confrelid
                  LEFT JOIN pg_catalog.pg_namespace fn on fn.oid = fc.relnamespace

                  WHERE a.atttypid <> 0::oid AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%') AND c.relkind = 'r')
              UNION
              (SELECT
                  t.viewname as TableName,
                  n.nspname AS TableSchema,
                  a.attname as ColumnName,
                  pg_catalog.format_type(a.atttypid, NULL) AS DbDataType,
                  pg_catalog.col_description(a.attrelid, a.attnum) AS ColumnComment,
                  (a.attnotnull = FALSE) AS IsNullable,
                  null AS PrimaryKeyConstraintName,
                  null AS ForeignKeyConstraintName,
                  null as ForeignKeyReferenceTableName,
                  null as ForeignKeyReferenceColumnName,
                  null as ForeignKeyReferenceSchemaName,
                  true as IsViewColumn

                  FROM pg_catalog.pg_class c
                  JOIN pg_catalog.pg_views t ON c.relname = t.viewname
                  JOIN pg_catalog.pg_attribute a ON c.oid = a.attrelid AND a.attnum > 0
                  JOIN pg_catalog.pg_namespace n ON n.oid = c.relnamespace

                  WHERE a.atttypid <> 0::oid
                    AND (n.nspname != 'information_schema' AND n.nspname NOT LIKE 'pg_%')
                    and not (n.nspname = 'public' and t.viewname = 'db_columns')
              )) ORDER BY TableSchema, IsViewColumn, TableName, ColumnName;
            ");

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
                    column.PropertyType = MetadataHelper.CreateSimpleType(column.DbDataType, column.IsNullable);
                    column.PropertyName = CodeGeneratorHelper.GetPropertyName(column.ColumnName);
                    column.IsPrimaryKey = column.PrimaryKeyConstraintName != null;
                    column.IsForeignKey = column.ForeignKeyConstraintName != null;
                    column.Comments = (column.ColumnComment ?? string.Empty).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);
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

        public async Task<List<FunctionMetadataModel>> GetFunctions()
        {
            var functions = await this.Connection.Query<FunctionMetadataModel>(@"
                 SELECT
                    n.nspname as SchemaName,
                    f.proname as FunctionName,
                    (case pg_get_function_identity_arguments(f.oid) when '' then null else pg_get_function_identity_arguments(f.oid) end) as FunctionArgumentsAsString,
                    (select t.typname::text from pg_type t where t.oid = f.prorettype) as FunctionReturnTypeName,

                    pg_get_functiondef(f.oid) as FunctionDefinition,

                    (SELECT d.description from pg_description d where d.classoid = 'pg_proc'::regclass and f.OID = d.objoid) as FunctionComment

                    FROM pg_catalog.pg_proc f
                    INNER JOIN pg_catalog.pg_namespace n ON (f.pronamespace = n.oid)
                    where f.prokind = 'f'
                    and n.nspname != 'information_schema'
                    AND n.nspname !~~ 'pg_%';
            ");

            foreach (var function in functions)
            {
                function.FunctionReturnType = MetadataHelper.CreateSimpleType(function.FunctionReturnTypeName, true);
                function.MethodName = CodeGeneratorHelper.GetPropertyName(function.FunctionName);
                function.Comments = (function.FunctionComment ?? string.Empty).Split("\r\n", StringSplitOptions.RemoveEmptyEntries);

                function.FunctionArguments = function.FunctionArgumentsAsString
                    .Split(',', StringSplitOptions.RemoveEmptyEntries).ToDictionary(
                        x => x.Split(' ', StringSplitOptions.RemoveEmptyEntries)[0].Trim(),
                        x => MetadataHelper.CreateSimpleType(string.Join(" ", x.Split(' ', StringSplitOptions.RemoveEmptyEntries).Skip(1)), true)
                    );
            }

            return functions;
        }
    }

    public static class MetadataHelper
    {
        public static SimpleType CreateSimpleType(string dbDataType, bool isNullable)
        {
            var simpleType = new SimpleType
            {
                DbDataType = dbDataType,
                IsNullable = isNullable,
                ClrTypeName = CodeGeneratorHelper.GetClrType(dbDataType, isNullable),
                ClrNullableTypeName = CodeGeneratorHelper.GetNullablePropertyType(dbDataType),
                Linq2DbDataTypeName = CodeGeneratorHelper.GetLinq2DbDataType(dbDataType),
                NpgsqlDbTypeName = CodeGeneratorHelper.GetNpgsqlDbType(dbDataType).GetNpgsqlDbTypeLiteral(),
            };

            simpleType.ClrNonNullableTypeName = simpleType.ClrTypeName.Trim('?');
            simpleType.IsClrValueType = simpleType.ClrTypeName != "string";
            simpleType.IsClrNullableType = simpleType.ClrTypeName != "string" && isNullable;
            simpleType.IsClrReferenceType = simpleType.ClrTypeName == "string" || isNullable;

            return simpleType;
        }
    }
}
