namespace Newsgirl.Shared
{
    using Autofac;

    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>().InstancePerLifetimeScope();
            builder.RegisterType<DbTransactionServiceImpl>().As<DbTransactionService>().InstancePerLifetimeScope();
            builder.RegisterType<DateProviderImpl>().As<DateProvider>().SingleInstance();
            builder.RegisterType<RngProviderImpl>().As<RngProvider>().SingleInstance();
            builder.RegisterType<RequestInfoMiddleware>();

            base.Load(builder);
        }
    }
}
