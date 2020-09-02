using System.Runtime.CompilerServices;

[assembly: InternalsVisibleTo("PgNet.Tests")]

namespace Newsgirl.Shared.Postgres
{
    using System.Linq;
    using Npgsql;

    public partial class DbService<TPocos> : IDbService<TPocos>
        where TPocos : IDbPocos<TPocos>, new()
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

        internal IQueryable<T> GetTable<T>()
            where T : class, IReadOnlyPoco<T>
        {
            return this.linqProvider.GetTable<T>();
        }

        public void Dispose()
        {
            this.linqProvider.Dispose();
        }
    }
}
