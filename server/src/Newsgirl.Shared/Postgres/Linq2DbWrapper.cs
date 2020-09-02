namespace Newsgirl.Shared.Postgres
{
    using System;
    using System.Linq;
    using LinqToDB.Data;
    using LinqToDB.DataProvider.PostgreSQL;
    using Npgsql;

    internal class Linq2DbWrapper : IDisposable, ILinqProvider
    {
        private readonly NpgsqlConnection connection;

        private DataConnection linq2Db;

        public Linq2DbWrapper(NpgsqlConnection connection)
        {
            this.connection = connection;
        }

        public IQueryable<T> GetTable<T>()
            where T : class, IReadOnlyPoco<T>
        {
            if (this.linq2Db == null)
            {
                this.linq2Db = new DataConnection(
                    new PostgreSQLDataProvider(),
                    this.connection,
                    false
                );
            }

            return this.linq2Db.GetTable<T>();
        }

        public void Dispose()
        {
            this.linq2Db?.Dispose();
        }
    }
}
