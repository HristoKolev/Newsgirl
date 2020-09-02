namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Collections.Generic;
    using System.Data;
    using System.Threading;
    using System.Threading.Tasks;
    using Npgsql;
    using NpgsqlTypes;

    // ReSharper disable once UnusedTypeParameter
    public partial class DbService<TPocos>
    {
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
    }
}
