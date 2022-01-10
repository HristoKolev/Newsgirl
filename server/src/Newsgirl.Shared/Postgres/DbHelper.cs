namespace Newsgirl.Shared.Postgres
{
    using Npgsql;

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
                IncludeErrorDetail = true,
            };

            return new NpgsqlConnection(builder.ToString());
        }
    }

    public class DbService : DbService<DbPocos>, IDbService
    {
        public DbService(NpgsqlConnection dbConnection) : base(dbConnection) { }
    }

    public interface IDbService : IDbService<DbPocos> { }
}
