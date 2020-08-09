namespace Newsgirl.Shared
{
    using System;
    using Autofac;
    
    public interface InstanceProvider
    {
        object Get(Type type);
        
        T Get<T>();
    }
    
    public class LifetimeScopeInstanceProvider : InstanceProvider
    {
        private readonly ILifetimeScope lifetimeScope;

        public LifetimeScopeInstanceProvider(ILifetimeScope lifetimeScope)
        {
            this.lifetimeScope = lifetimeScope;
        }
        
        public object Get(Type type)
        {
            return this.lifetimeScope.Resolve(type);
        }

        public T Get<T>()
        {
            return this.lifetimeScope.Resolve<T>();
        }
    }
}
