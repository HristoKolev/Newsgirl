namespace Newsgirl.WebServices.Infrastructure.Data
{
    using Npgsql;
    using PgNet;
    
    public interface IDbService : IDbService<DbPocos>
    {
    }

    public class DbService : DbService<DbPocos>, IDbService
    {
        public DbService(NpgsqlConnection dbConnection)
            : base(dbConnection)
        {
        }
    }
}