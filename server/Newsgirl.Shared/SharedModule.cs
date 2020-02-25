using Autofac;

namespace Newsgirl.Shared
{
    public class SharedModule : Module
    {
        protected override void Load(ContainerBuilder builder)
        {
            builder.RegisterType<SystemSettingsService>();

            base.Load(builder);
        }
    }
}