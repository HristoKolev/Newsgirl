namespace Newsgirl.Shared.Infrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;
    using Data;
    using Npgsql;

    public class TransactionService : ITransactionService
    {
        private readonly DbService db;

        public TransactionService(DbService db)
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

    public interface ITransactionService
    {
        Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default);

        Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default);
    }
}
