using System;
using System.Threading;
using System.Threading.Tasks;
using Newsgirl.Shared.Data;
using Npgsql;

namespace Newsgirl.Shared.Infrastructure
{
    public class TransactionService : ITransactionService
    {
        private readonly DbService db;

        public TransactionService(DbService db)
        {
            this.db = db;
        }

        public Task ExecuteInTransaction(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default) => this.db.ExecuteInTransaction(body, cancellationToken);

        public Task ExecuteInTransactionAndCommit(Func<Task> body, CancellationToken cancellationToken = default) =>
            this.db.ExecuteInTransactionAndCommit(body, cancellationToken);

        public Task ExecuteInTransactionAndCommit(Func<NpgsqlTransaction, Task> body,
            CancellationToken cancellationToken = default) =>
            this.db.ExecuteInTransactionAndCommit(body, cancellationToken);
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