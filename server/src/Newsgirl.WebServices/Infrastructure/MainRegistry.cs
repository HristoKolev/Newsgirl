namespace Newsgirl.WebServices.Infrastructure
{
    using System.Linq;
    using System.Reflection;

    using Data;

    using Npgsql;

    using StructureMap;

    public class MainRegistry : Registry
    {
        public MainRegistry()
        {
            this.DataAccess();
            this.Infrastructure();
        }

        private void DataAccess()
        {
            this.For<NpgsqlConnection>()
                .Use("Postgres connection.", ctx => new NpgsqlConnection(Global.AppConfig.ConnectionString))
                .ContainerScoped();

            this.For<IDbService>().Use<DbService>();
            this.For<SystemSettings>().Use(() => Global.Settings).Singleton();
        }

        private void Infrastructure()
        {
            this.For<MainLogger>().Singleton();

            // Handlers
            foreach (var handler in Global.Handlers.GetAllHandlers())
            {
                this.For(handler.HandlerType).ContainerScoped();
            }

            // Services
            var serviceTypes = Assembly.GetExecutingAssembly().DefinedTypes.Select(info => info.AsType())
                                       .Where(type => type.IsClass && type.Name.EndsWith("Service"))
                                       .ToList();

            foreach (var type in serviceTypes)
            {
                this.For(type).ContainerScoped();
            }
        }
    }
}