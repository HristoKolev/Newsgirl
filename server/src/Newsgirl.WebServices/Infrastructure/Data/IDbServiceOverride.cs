using Npgsql;
using PgNet;

namespace Newsgirl.WebServices.Infrastructure.Data
{
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