namespace Newsgirl.Shared
{
    using Autofac;

    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>().InstancePerLifetimeScope();
            builder.RegisterType<DateTimeServiceImpl>().As<DateTimeService>().InstancePerLifetimeScope();

            builder.RegisterType<RngServiceImpl>().As<RngService>().InstancePerLifetimeScope();
            builder.RegisterType<PasswordServiceImpl>().As<PasswordService>().InstancePerLifetimeScope();

            base.Load(builder);
        }
    }
}
