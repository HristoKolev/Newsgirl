namespace Newsgirl.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;
    using Postgres;

    /// <summary>
    /// Use to control database transactions.
    /// </summary>
    public interface DbTransactionService
    {
        /// <summary>
        /// Invokes the given delegate instance in a transaction. You have to commit the transaction manually.
        /// </summary>
        Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes the given delegate instance in a transaction and commits it automatically.
        /// </summary>
        Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default);

        /// <summary>
        /// Invokes the given delegate instance in a transaction and commits it automatically.
        /// </summary>
        Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default);
    }

    public class DbTransactionServiceImpl : DbTransactionService
    {
        private readonly IDbService db;

        public DbTransactionServiceImpl(IDbService db)
        {
            this.db = db;
        }

        public Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransaction(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body, CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransactionAndCommit(body, cancellationToken);
        }

        public Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default)
        {
            return this.db.ExecuteInTransactionAndCommit(body, cancellationToken);
        }
    }
}
