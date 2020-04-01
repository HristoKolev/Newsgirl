using Autofac;
using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared
{
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
