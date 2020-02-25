using Autofac;
using Newsgirl.Shared;
using Newsgirl.Shared.Data;

namespace Newsgirl.Fetcher
{
    public class FetcherModule : Autofac.Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.Register((c, p) => 
                DbFactory.CreateConnection(Global.AppConfig.ConnectionString))
                .InstancePerLifetimeScope();

            builder.RegisterType<DbService>()
                .InstancePerLifetimeScope();
            
            builder.RegisterType<FeedFetcher>();
            
            base.Load(builder);
        }
    }
    
    public static class IoCFactory
    {
        public static IContainer Create()
        {
            var builder = new ContainerBuilder();

            builder.RegisterModule<SharedModule>();
            builder.RegisterModule<FetcherModule>();

            return builder.Build();
        }
    }
}