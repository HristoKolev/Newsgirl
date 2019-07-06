namespace Newsgirl.WebServices.Infrastructure
{
    using System.IO;
    using System.Linq;
    using System.Reflection;
    using System.Security.Cryptography.X509Certificates;
    using System.Threading.Tasks;

    using Api;

    using Autofac;

    using Data;

    public class MainModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            // Data Access
            builder.Register(x => DbHelper.CreateConnection()).InstancePerLifetimeScope();
            builder.RegisterType<DbService>().As<IDbService>().InstancePerLifetimeScope();
            
            // Infrastructure
            builder.Register(x => MainLogger.Instance);
            builder.Register(x => Global.Settings);
            builder.RegisterType<TypeResolver>().InstancePerLifetimeScope();
            builder.Register(x => new ObjectPool<X509Certificate2>(CreateCertificate));
            builder.RegisterType<DirectApiClient>().As<IApiClient>().InstancePerLifetimeScope();
            
            // Handlers
            foreach (var handler in Global.Handlers.GetAllHandlers())
            {
                builder.RegisterType(handler.HandlerType).InstancePerLifetimeScope();
            }
            
            // Services
            var serviceTypes = Assembly.GetExecutingAssembly()
                                       .DefinedTypes
                                       .Select(info => info.AsType())
                                       .Where(type => type.IsClass && type.Name.EndsWith("Service"))
                                       .ToList();

            foreach (var serviceType in serviceTypes)
            {
                builder.RegisterType(serviceType).InstancePerLifetimeScope();
            }

            // Commands
            foreach (var commandType in CliParser.AllCommands.Select(x => x.CommandType))
            {
                builder.RegisterType(commandType).InstancePerLifetimeScope();
            }
            
            base.Load(builder);
        }

        private static async Task<X509Certificate2> CreateCertificate()
        {
            string certificatePath = Path.Combine(Global.DataDirectory, "certificate.pfx");
            
            var certBytes = await File.ReadAllBytesAsync(certificatePath);
                
            return new X509Certificate2(certBytes);
        }
    }
}