namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.Data;
    using System.Linq;
    using System.Reflection;
    using System.Text;
    using System.Threading;
    using System.Threading.Tasks;
    using LinqToDB;
    using LinqToDB.Data;
    using LinqToDB.DataProvider.PostgreSQL;
    using Npgsql;
    using NpgsqlTypes;

    public class DbService<TPocos> : IDbService<TPocos> where TPocos : IDbPocos<TPocos>, new()
    {
        private readonly NpgsqlConnection connection;

        private readonly Linq2DbWrapper linqProvider;

        public TPocos Poco { get; }

        public DbService(NpgsqlConnection connection)
        {
            this.connection = connection;
            this.linqProvider = new Linq2DbWrapper(connection);
            this.Poco = new TPocos
            {
                LinqProvider = this.linqProvider,
            };
        }

        public IQueryable<T> GetTable<T>() where T : class, IReadOnlyPoco<T>
        {
            return this.linqProvider.GetTable<T>();
        }

        public void Dispose()
        {
            this.linqProvider.Dispose();
        }

        public async Task<NpgsqlTransaction> BeginTransaction()
        {
            if (this.connection.State == ConnectionState.Closed)
            {
                await this.connection.OpenAsync();
            }

            return await this.connection.BeginTransactionAsync();
        }

        public Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default)
        {
            return this.connection.ExecuteInTransaction(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default)
        {
            return this.connection.ExecuteInTransactionAndCommit(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default)
        {
            return this.connection.ExecuteInTransactionAndCommit(body, cancellationToken);
        }

        public Task<int> ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters)
        {
            return this.connection.ExecuteNonQuery(sql, parameters);
        }

        public Task<T> ExecuteScalar<T>(string sql, params NpgsqlParameter[] parameters)
        {
            return this.connection.ExecuteScalar<T>(sql, parameters);
        }

        public NpgsqlParameter CreateParameter<T>(string parameterName, T value)
        {
            return this.connection.CreateParameter(parameterName, value);
        }

        public NpgsqlParameter CreateParameter<T>(string parameterName, T value, NpgsqlDbType dbType)
        {
            return this.connection.CreateParameter(parameterName, value, dbType);
        }

        public Task<List<T>> Query<T>(string sql, params NpgsqlParameter[] parameters) where T : new()
        {
            return this.connection.Query<T>(sql, parameters);
        }

        public Task<T> QueryOne<T>(string sql, params NpgsqlParameter[] parameters) where T : class, new()
        {
            return this.connection.QueryOne<T>(sql, parameters);
        }

        public Task<int> BulkInsert<T>(IEnumerable<T> pocos, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();
            var columns = metadata.Columns;

            var sqlBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            sqlBuilder.Append("INSERT INTO \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" (");

            bool headerFirstRun = true;

            // ReSharper disable once ForCanBeConvertedToForeach
            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (!column.IsPrimaryKey)
                {
                    if (headerFirstRun)
                    {
                        sqlBuilder.Append("\"");
                        headerFirstRun = false;
                    }
                    else
                    {
                        sqlBuilder.Append(", \"");
                    }

                    sqlBuilder.Append(column.ColumnName);
                    sqlBuilder.Append('"');
                }
            }

            sqlBuilder.Append(") VALUES ");

            var allParameters = new List<NpgsqlParameter>();

            // PARAMETERS
            int paramIndex = 0;

            bool recordsFirstRun = true;

            foreach (var record in pocos)
            {
                if (!recordsFirstRun)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append("\n(");
                recordsFirstRun = false;

                var parameters = metadata.GenerateParameters(record);

                allParameters.AddRange(parameters);

                for (int i = 0; i < parameters.Length; i++)
                {
                    if (i != 0)
                    {
                        sqlBuilder.Append(", ");
                    }

                    int currentIndex = paramIndex++;
                    var parameter = parameters[i];
                    parameter.ParameterName = "p" + currentIndex;

                    sqlBuilder.Append("@p");
                    sqlBuilder.Append(currentIndex);
                }

                sqlBuilder.Append(")");
            }

            sqlBuilder.Append(";");

            string sql = sqlBuilder.ToString();

            return this.connection.ExecuteNonQuery(sql, allParameters, cancellationToken);
        }

        public Task<int> Delete<T>(T model, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            return this.Delete<T>(pk, cancellationToken);
        }

        public Task<int> Delete<T>(int[] ids, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            if (ids.Length == 0)
            {
                return Task.FromResult(0);
            }

            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", ids),
            };

            string sql = $"DELETE FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = any(@pk);";

            return this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public Task<int> Delete<T>(int id, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", id),
            };

            string sql = $"DELETE FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = @pk;";

            return this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public async Task<int> Insert<T>(T model, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = await this.InsertWithoutMutating(model, cancellationToken);

            metadata.SetPrimaryKey(model, pk);

            return pk;
        }

        public Task<int> InsertWithoutMutating<T>(T model, CancellationToken cancellationToken = default) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            var (columnNames, parameters) = metadata.GetAllColumns(model);

            var sqlBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            sqlBuilder.Append("INSERT INTO \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" (");

            for (int i = 0; i < columnNames.Length; i++)
            {
                if (i != 0)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append('"');
                sqlBuilder.Append(columnNames[i]);
                sqlBuilder.Append('"');
            }

            sqlBuilder.Append(")\n VALUES (");

            for (int i = 0; i < parameters.Length; i++)
            {
                if (i != 0)
                {
                    sqlBuilder.Append(", ");
                }

                sqlBuilder.Append("@p");
                sqlBuilder.Append(i);

                parameters[i].ParameterName = "p" + i;
            }

            // STATEMENT FOOTER
            sqlBuilder.Append(") RETURNING \"");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append("\";");

            string sql = sqlBuilder.ToString();

            return this.connection.ExecuteScalar<int>(sql, parameters, cancellationToken);
        }

        public async Task<int> Save<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            if (metadata.IsNew(model))
            {
                return await this.Insert(model, cancellationToken);
            }

            await this.Update(model, cancellationToken);

            return metadata.GetPrimaryKey(model);
        }

        public Task<int> Update<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            if (pk == default)
            {
                throw new ApplicationException("Cannot update a model with primary key of 0.");
            }

            var (columnNames, parameters) = metadata.GetAllColumns(model);

            if (columnNames.Length == 0)
            {
                return Task.FromResult(0); // nothing to update?
            }

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append("UPDATE \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" SET ");

            for (int i = 0; i < columnNames.Length; i++)
            {
                string columnName = columnNames[i];
                var parameter = parameters[i];

                string paramName = "@p" + i;

                sqlBuilder.Append("\n\"");
                sqlBuilder.Append(columnName);
                sqlBuilder.Append("\" = ");
                sqlBuilder.Append(paramName);

                if (i != columnNames.Length - 1)
                {
                    sqlBuilder.Append(',');
                }

                parameter.ParameterName = paramName;
            }

            sqlBuilder.Append("\nWHERE \"");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append("\" = @pk;");

            string sql = sqlBuilder.ToString();

            var allParameters = parameters.Concat(new[]
            {
                this.CreateParameter("pk", pk),
            });

            return this.connection.ExecuteNonQuery(sql, allParameters, cancellationToken);
        }

        public async Task<int> UpdateChangesOnly<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>, new()
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            int pk = metadata.GetPrimaryKey(model);

            if (pk == default)
            {
                throw new ApplicationException("Cannot update a model with primary key of 0.");
            }

            string selectSql =
                $"select * from \"{metadata.TableSchema}\".\"{metadata.TableName}\" where \"{metadata.PrimaryKeyColumnName}\" = @pk FOR UPDATE;";

            var selectParameters = new[]
            {
                this.CreateParameter("pk", pk),
            };

            var currentInstance = await this.connection.QueryOne<T>(selectSql, selectParameters, cancellationToken);

            if (currentInstance == null)
            {
                throw new ApplicationException("Cannot update record: record does not exists."); // no record to update?
            }

            var (names, parameters) = metadata.GetColumnChanges(currentInstance, model);

            if (names.Count == 0)
            {
                return 0; // nothing to update?
            }

            var sqlBuilder = new StringBuilder();

            sqlBuilder.Append("UPDATE \"");
            sqlBuilder.Append(metadata.TableSchema);
            sqlBuilder.Append("\".\"");
            sqlBuilder.Append(metadata.TableName);
            sqlBuilder.Append("\" SET ");

            for (int i = 0; i < names.Count; i++)
            {
                string name = names[i];
                var parameter = parameters[i];

                string paramName = "@p" + i;

                sqlBuilder.Append('\n');
                sqlBuilder.Append(name);
                sqlBuilder.Append(" = ");
                sqlBuilder.Append(paramName);

                if (i != names.Count - 1)
                {
                    sqlBuilder.Append(',');
                }

                parameter.ParameterName = paramName;
            }

            sqlBuilder.Append("\nWHERE ");
            sqlBuilder.Append(metadata.PrimaryKeyColumnName);
            sqlBuilder.Append(" = @pk;");

            parameters.Add(this.CreateParameter("pk", pk));

            string sql = sqlBuilder.ToString();

            return await this.connection.ExecuteNonQuery(sql, parameters, cancellationToken);
        }

        public Task<T> FindByID<T>(int id, CancellationToken cancellationToken = default) where T : class, IPoco<T>, new()
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();

            string tableSchema = metadata.TableSchema;
            string tableName = metadata.TableName;
            string primaryKeyName = metadata.PrimaryKeyColumnName;

            var parameters = new[]
            {
                this.CreateParameter("pk", id),
            };

            string sql = $"SELECT * FROM \"{tableSchema}\".\"{tableName}\" WHERE \"{primaryKeyName}\" = @pk;";

            return this.connection.QueryOne<T>(sql, parameters, cancellationToken);
        }

        public async Task Copy<T>(IEnumerable<T> pocos) where T : IPoco<T>
        {
            var metadata = DbCodeGenerator.GetMetadata<T>();
            var columns = metadata.Columns;

            var copyHeaderBuilder = new StringBuilder(128);

            // STATEMENT HEADER
            copyHeaderBuilder.Append("COPY \"");
            copyHeaderBuilder.Append(metadata.TableSchema);
            copyHeaderBuilder.Append("\".\"");
            copyHeaderBuilder.Append(metadata.TableName);
            copyHeaderBuilder.Append("\" (");

            bool headerFirstRun = true;

            for (int i = 0; i < columns.Count; i++)
            {
                var column = columns[i];

                if (!column.IsPrimaryKey)
                {
                    if (headerFirstRun)
                    {
                        copyHeaderBuilder.Append("\"");
                        headerFirstRun = false;
                    }
                    else
                    {
                        copyHeaderBuilder.Append(", \"");
                    }

                    copyHeaderBuilder.Append(column.ColumnName);
                    copyHeaderBuilder.Append('"');
                }
            }

            copyHeaderBuilder.Append(") FROM STDIN (FORMAT BINARY)");

            await using (var importer = this.connection.BeginBinaryImport(copyHeaderBuilder.ToString()))
            {
                foreach (var poco in pocos)
                {
                    await importer.StartRowAsync();

                    metadata.WriteToImporter(importer, poco);
                }

                await importer.CompleteAsync();
            }
        }
    }

    /// <summary>
    /// Interface for all generated database specific API types.
    /// </summary>
    public interface IDbPocos<TDbPocos> where TDbPocos : IDbPocos<TDbPocos>, new()
    {
        ILinqProvider LinqProvider { set; }
    }

    public interface ILinqProvider
    {
        IQueryable<T> GetTable<T>() where T : class, IReadOnlyPoco<T>;
    }

    public class Linq2DbWrapper : IDisposable, ILinqProvider
    {
        private readonly NpgsqlConnection connection;

        private DataConnection linq2Db;

        public Linq2DbWrapper(NpgsqlConnection connection)
        {
            this.connection = connection;
        }

        public IQueryable<T> GetTable<T>() where T : class, IReadOnlyPoco<T>
        {
            this.linq2Db ??= new DataConnection(new PostgreSQLDataProvider(), this.connection, false);

            return this.linq2Db.GetTable<T>();
        }

        public void Dispose()
        {
            this.linq2Db?.Dispose();
        }
    }

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

            return arg => property!.GetMethod!.Invoke(arg, Array.Empty<object>());
        }

        private static Action<T, object> GetSetter<T>(string propertyName)
        {
            var instanceType = typeof(T);

            var property = instanceType.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Instance);

            return (obj, value) => property!.SetMethod!.Invoke(obj, new[] {value});
        }

        public static Func<T, T> GetClone<T>() where T : new()
        {
            var instanceType = typeof(T);

            return obj =>
            {
                var instance = new T();

                foreach (var fieldInfo in instanceType.GetFields(BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Public))
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
                        Value = property!.GetValue(poco) ?? DBNull.Value,
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

                    var value = property!.GetValue(poco);

                    if (value == null)
                    {
                        importer.WriteNull();
                    }
                    else
                    {
                        var type = IsNullableType(property.PropertyType)
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

        public static Func<TPoco, ValueTuple<string[], NpgsqlParameter[]>> GetGetAllColumns<TPoco>(
            TableMetadataModel<TPoco> metadata) where TPoco : IPoco<TPoco>
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
                        Value = property!.GetValue(poco) ?? DBNull.Value,
                        NpgsqlDbType = column.PropertyType.NpgsqlDbType,
                    });
                }

                return (names.ToArray(), list.ToArray());
            };
        }

        public static Func<TPoco, TPoco, ValueTuple<List<string>, List<NpgsqlParameter>>> GetGetColumnChanges<TPoco>(
            TableMetadataModel<TPoco> metadata) where TPoco : IPoco<TPoco>
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

                    var value1 = property!.GetValue(obj1);
                    var value2 = property!.GetValue(obj2);

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
                var dict = new Dictionary<string, Action<T, object>>(StringComparer.OrdinalIgnoreCase);

                foreach (var x in type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.SetMethod != null))
                {
                    dict.Add(propertyNameToColumnName(x.Name), GetSetter<T>(x.Name));
                }

                return dict;
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

        public static bool StupidEquals(object a, object b)
        {
            return a != null && (a.GetType().IsValueType || a is string) ? Equals(a, b) : ReferenceEquals(a, b);
        }
    }

    /// <summary>
    /// All Poco types implement this interface, either directly or through the <see cref="IPoco{T}" /> Interface.
    /// </summary>
    /// <typeparam name="T">The poco type.</typeparam>
    // ReSharper disable once UnusedTypeParameter
    public interface IReadOnlyPoco<T> { }

    /// <summary>
    /// Interface for all Poco types generated from database Tables, omitting types generated from Views.
    /// </summary>
    public interface IPoco<T> : IReadOnlyPoco<T> { }

    /// <summary>
    /// The main API for the database access interface.
    /// </summary>
    /// <typeparam name="TPocos">The type for the database specific generated APIs.</typeparam>
    public interface IDbService<out TPocos> : IDisposable where TPocos : IDbPocos<TPocos>, new()
    {
        /// <summary>
        /// Calls `BeginTransaction` on the connection and returns the result.
        /// </summary>
        Task<NpgsqlTransaction> BeginTransaction();

        /// <summary>
        /// Inserts several records in single query.
        /// </summary>
        Task<int> BulkInsert<T>(IEnumerable<T> pocos, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        /// Deletes a record by its PrimaryKey.
        /// </summary>
        Task<int> Delete<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        /// Deletes records from a table by their IDs.
        /// </summary>
        Task<int> Delete<T>(int[] ids, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        /// Deletes a record by ID.
        /// </summary>
        Task<int> Delete<T>(int id, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        /// Starts a transaction and runs the `body` function passing the native <see cref="NpgsqlTransaction" /> object.
        /// </summary>
        Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a transaction, runs the `body` function
        /// and if it does not manually rollback throw an exception - commits the transaction.
        /// </summary>
        Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Starts a transaction, runs the `body` function passing the native <see cref="NpgsqlTransaction" /> object
        /// and if it does not manually rollback throw an exception - commits the transaction.
        /// </summary>
        Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Executes a query and returns the rows affected.
        /// </summary>
        Task<int> ExecuteNonQuery(string sql, params NpgsqlParameter[] parameters);

        /// <summary>
        /// Executes a query and returns a scalar value of type T.
        /// It throws if the result set does not have exactly one column and one row.
        /// It throws if the return value is 'null' and the type T is a value type.
        /// </summary>
        Task<T> ExecuteScalar<T>(string sql, params NpgsqlParameter[] parameters);

        /// <summary>
        /// Inserts a record and attaches it's ID to the poco object.
        /// </summary>
        Task<int> Insert<T>(T model, CancellationToken cancellationToken = default) where T : IPoco<T>;

        /// <summary>
        /// Inserts a record and returns its ID.
        /// </summary>
        Task<int> InsertWithoutMutating<T>(T model, CancellationToken cancellationToken = default) where T : IPoco<T>;

        /// <summary>
        /// Creates a parameter of type T with NpgsqlDbType from the default type map 'defaultNpgsqlDbTypeMap'.
        /// </summary>
        NpgsqlParameter CreateParameter<T>(string parameterName, T value);

        /// <summary>
        /// Creates a parameter of type T by explicitly specifying NpgsqlDbType.
        /// </summary>
        NpgsqlParameter CreateParameter<T>(string parameterName, T value, NpgsqlDbType dbType);

        /// <summary>
        /// Executes a query and returns objects
        /// </summary>
        Task<List<T>> Query<T>(string sql, params NpgsqlParameter[] parameters) where T : new();

        /// <summary>
        /// Returns one object of type T.
        /// If there are no rows then returns 'null';
        /// If there is more that one row then throws.
        /// </summary>
        Task<T> QueryOne<T>(string sql, params NpgsqlParameter[] parameters) where T : class, new();

        /// <summary>
        /// Saves a record to the database.
        /// If the poco object has a positive primary key it updates it.
        /// If the primary key value is 0 it inserts the record.
        /// Returns the record's primary key value.
        /// </summary>
        Task<int> Save<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>;

        /// <summary>
        /// Updates a record by its ID.
        /// </summary>
        Task<int> Update<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>;

        /// <summary>
        /// Updates a record by its ID.
        /// Only updates the changed rows.
        /// </summary>
        Task<int> UpdateChangesOnly<T>(T model, CancellationToken cancellationToken = default) where T : class, IPoco<T>, new();

        /// <summary>
        /// Returns a record by its ID.
        /// </summary>
        Task<T> FindByID<T>(int id, CancellationToken cancellationToken = default) where T : class, IPoco<T>, new();

        /// <summary>
        /// The database specific API.
        /// </summary>
        TPocos Poco { get; }
    }

    public class TableMetadataModel<T> : TableMetadataModel
    {
        /// <summary>
        /// Clones the current object and returns the clone.
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public Func<T, T> Clone;

        /// <summary>
        /// Returns the primary key for the table.
        /// </summary>
        public Func<T, int> GetPrimaryKey;

        /// <summary>
        /// Returns true if the record hasn't been inserted to the database yet.
        /// </summary>
        public Func<T, bool> IsNew;

        /// <summary>
        /// Sets the primary key for the table.
        /// </summary>
        public Action<T, int> SetPrimaryKey;

        /// <summary>
        /// Generates a parameter for every non Primary Key column in the table.
        /// </summary>
        public Func<T, NpgsqlParameter[]> GenerateParameters { get; set; }

        /// <summary>
        /// Generates a parameter for every column in the table.
        /// </summary>
        public Func<T, ValueTuple<string[], NpgsqlParameter[]>> GetAllColumns { get; set; }

        /// <summary>
        /// Generates the changes between 2 instances of the poco class.
        /// Returns the names of the changed columns and parameters for every column value.
        /// </summary>
        public Func<T, T, ValueTuple<List<string>, List<NpgsqlParameter>>> GetColumnChanges { get; set; }

        /// <summary>
        /// Writes the poco object to the binary importer.
        /// </summary>
        public Action<NpgsqlBinaryImporter, T> WriteToImporter { get; set; }
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

        public string PrimaryKeyPropertyName { get; set; }

        public string TableName { get; set; }

        public string TableSchema { get; set; }

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

        public string DbDataType { get; set; }

        public string ForeignKeyConstraintName { get; set; }

        public string ForeignKeyReferenceColumnName { get; set; }

        public string ForeignKeyReferenceSchemaName { get; set; }

        public string ForeignKeyReferenceTableName { get; set; }

        public bool IsForeignKey { get; set; }

        public bool IsNullable { get; set; }

        public bool IsPrimaryKey { get; set; }

        public string PrimaryKeyConstraintName { get; set; }

        public string PropertyName { get; set; }

        public string TableName { get; set; }

        public string TableSchema { get; set; }

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

        public Dictionary<string, SimpleType> FunctionArguments { get; set; }
    }

    public class SimpleType
    {
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

        public Type ClrType { get; set; }

        public Type ClrNonNullableType { get; set; }

        public Type ClrNullableType { get; set; }

        public DataType Linq2DbDataType { get; set; }

        public NpgsqlDbType NpgsqlDbType { get; set; }
    }
}
