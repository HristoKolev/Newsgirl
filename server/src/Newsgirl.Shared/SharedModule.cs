namespace Newsgirl.Shared
{
    using Autofac;

    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>().InstancePerLifetimeScope();
            builder.RegisterType<DbTransactionServiceImpl>().As<DbTransactionService>().InstancePerLifetimeScope();
            builder.RegisterType<DateTimeServiceImpl>().As<DateTimeService>().InstancePerLifetimeScope();

            builder.RegisterType<RngServiceImpl>().As<RngService>().SingleInstance();
            builder.RegisterType<PasswordServiceImpl>().As<PasswordService>().SingleInstance();

            base.Load(builder);
        }
    }
}
