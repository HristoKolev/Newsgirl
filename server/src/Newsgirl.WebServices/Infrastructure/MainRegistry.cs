namespace Newsgirl.WebServices.Infrastructure
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Api;

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
                .Use("Postgres connection.", ctx => DbHelper.CreateConnection())
                .ContainerScoped();

            this.For<IDbService>().Use<DbService>();
            this.For<SystemSettings>().Use(() => Global.Settings).Singleton();
        }

        private async Task<X509Certificate2> CreateCertificate()
        {
            var certBytes = await File.ReadAllBytesAsync(Path.Combine(Global.DataDirectory, "certificate.pfx"));
                
            return new X509Certificate2(certBytes);
        } 
        
        private void Infrastructure()
        {
            this.For<MainLogger>().Singleton();
            this.For<TypeResolver>().ContainerScoped();
            this.For<ObjectPool<X509Certificate2>>()
                .Use(ctx => new ObjectPool<X509Certificate2>(this.CreateCertificate)).Singleton();
            this.For<IApiClient>().Use<DirectApiClient>();

            // Handlers
            foreach (var handler in Global.Handlers.GetAllHandlers())
            {
                this.For(handler.HandlerType).ContainerScoped();
            }

            // Services
            var serviceTypes = Assembly.GetExecutingAssembly()
                               .DefinedTypes
                               .Select(info => info.AsType())
                               .Where(type => type.IsClass && type.Name.EndsWith("Service"))
                               .ToList();

            foreach (var type in serviceTypes)
            {
                this.For(type).ContainerScoped();
            }
        }
    }
}