namespace Newsgirl.Shared
{
    using Autofac;
    using Logging;

    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>().InstancePerLifetimeScope();
            builder.RegisterType<DateTimeServiceImpl>().As<DateTimeService>().InstancePerLifetimeScope();
            builder.RegisterType<LogPreprocessor>().InstancePerLifetimeScope();

            builder.RegisterType<RngServiceImpl>().As<RngService>().SingleInstance();
            builder.RegisterType<PasswordServiceImpl>().As<PasswordService>().SingleInstance();

            base.Load(builder);
        }
    }
}
