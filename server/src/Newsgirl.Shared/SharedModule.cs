namespace Newsgirl.Shared
{
    using Autofac;
    using Infrastructure;

    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<TransactionService>().As<ITransactionService>()
                .InstancePerLifetimeScope();

            builder.RegisterType<DateProvider>().As<IDateProvider>()
                .SingleInstance();

            base.Load(builder);
        }
    }
}
