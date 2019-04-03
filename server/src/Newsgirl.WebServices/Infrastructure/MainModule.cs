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
            builder.Register(x => Global.Settings).InstancePerLifetimeScope();
            
            // Infrastructure
            builder.Register(x => Global.Log);
            builder.RegisterType<TypeResolver>().InstancePerLifetimeScope();
            builder.Register(x => new ObjectPool<X509Certificate2>(this.CreateCertificate));
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

            foreach (var commandType in CliParser.AllCommands.Select(x => x.CommandType))
            {
                builder.RegisterType(commandType).InstancePerLifetimeScope();
            }
            
            base.Load(builder);
        }

        private async Task<X509Certificate2> CreateCertificate()
        {
            var certBytes = await File.ReadAllBytesAsync(Path.Combine(Global.DataDirectory, "certificate.pfx"));
                
            return new X509Certificate2(certBytes);
        }
    }
}