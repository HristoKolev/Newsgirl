namespace Newsgirl.WebServices.Infrastructure.Data
{
    using Npgsql;

    public static class DbHelper
    {
        /// <summary>
        /// Creates a new `NpgsqlConnection` connection.
        /// Uses a connection string from the application settings.
        /// </summary>
        public static NpgsqlConnection CreateConnection()
        {
            string connectionString = Global.AppConfig.ConnectionString;
            
            var builder = new NpgsqlConnectionStringBuilder(connectionString)
            {
                Enlist = false // Turn this off in order to save some perf. It disables the support for `TransactionScope`. 
            };
            
            return new NpgsqlConnection(builder.ToString());
        }
    }
}