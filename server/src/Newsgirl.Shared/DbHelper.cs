namespace Newsgirl.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;
    using PgNet;

    public static class DbFactory
    {
        /// <summary>
        /// Creates a new `NpgsqlConnection` connection.
        /// </summary>
        public static NpgsqlConnection CreateConnection(string connectionString)
        {
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false, // Turn this off in order to save some perf. It disables the support for `TransactionScope`.
            };

            return new NpgsqlConnection(builder.ToString());
        }
    }

    public class DbService : DbService<DbPocos>
    {
        public DbService(NpgsqlConnection dbConnection)
            : base(dbConnection) { }
    }

    /// <summary>
    /// Use to control database transactions.
    /// </summary>
    public interface DbTransactionService
    {
        Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default);
    }

    public class DbTransactionServiceImpl : DbTransactionService
    {
        private readonly DbService db;

        public DbTransactionServiceImpl(DbService db)
        {
            this.db = db;
        }

        public Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransaction(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransactionAndCommit(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransactionAndCommit(body, cancellationToken);
        }
    }
}
