namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using LinqToDB;
    using Npgsql;
    using NpgsqlTypes;

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
    public interface IDbService<TPocos> : IDisposable where TPocos : IDbPocos<TPocos>, new()
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
        ///     <para>Deletes records from a table by their IDs.</para>
        /// </summary>
        Task<int> Delete<T>(int[] ids, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        ///     <para>Deletes a record by ID.</para>
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
        Task<int> Insert<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

        /// <summary>
        /// Inserts a record and returns its ID.
        /// </summary>
        Task<int> InsertWithoutMutating<T>(T model, CancellationToken cancellationToken = default)
            where T : IPoco<T>;

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
        Task<List<T>> Query<T>(string sql, params NpgsqlParameter[] parameters)
            where T : new();

        /// <summary>
        /// Returns one object of type T.
        /// If there are no rows then returns 'null';
        /// If there is more that one row then throws.
        /// </summary>
        Task<T> QueryOne<T>(string sql, params NpgsqlParameter[] parameters)
            where T : class, new();

        /// <summary>
        /// Saves a record to the database.
        /// If the poco object has a positive primary key it updates it.
        /// If the primary key value is 0 it inserts the record.
        /// Returns the record's primary key value.
        /// </summary>
        Task<int> Save<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>;

        /// <summary>
        /// Updates a record by its ID.
        /// </summary>
        Task<int> Update<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>;

        /// <summary>
        /// Updates a record by its ID.
        /// Only updates the changed rows.
        /// </summary>
        Task<int> UpdateChangesOnly<T>(T model, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>, new();

        /// <summary>
        /// Returns a record by its ID.
        /// </summary>
        Task<T> FindByID<T>(int id, CancellationToken cancellationToken = default)
            where T : class, IPoco<T>, new();

        /// <summary>
        /// The database specific API.
        /// </summary>
        TPocos Poco { get; }
    }

    public class TableMetadataModel<T> : TableMetadataModel
    {
        /// <summary>
        ///     <para>Clones the current object and returns the clone.</para>
        /// </summary>
        // ReSharper disable once NotAccessedField.Global
        public Func<T, T> Clone;

        /// <summary>
        ///     <para>Returns the primary key for the table.</para>
        /// </summary>
        public Func<T, int> GetPrimaryKey;

        /// <summary>
        ///     <para>Returns true if the record hasn't been inserted to the database yet.</para>
        /// </summary>
        public Func<T, bool> IsNew;

        /// <summary>
        ///     <para>Sets the primary key for the table.</para>
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

    /// <summary>
    /// Interface for all generated Metadata types.
    /// </summary>
    public interface IDbMetadata { }

    /// <summary>
    /// Interface for all generated database specific API types.
    /// </summary>
    public interface IDbPocos<TDbPocos>
        where TDbPocos : IDbPocos<TDbPocos>, new()
    {
        ILinqProvider LinqProvider { set; }
    }

    public interface ILinqProvider
    {
        IQueryable<T> GetTable<T>()
            where T : class, IReadOnlyPoco<T>;
    }

    public class PocoTemplateContext
    {
        public List<TableMetadataModel> Tables { get; set; }

        public List<FunctionMetadataModel> Functions { get; set; }

        public string Namespace { get; set; }

        public string PocoClassName { get; set; }

        public string MetadataClassName { get; set; }
    }
}
