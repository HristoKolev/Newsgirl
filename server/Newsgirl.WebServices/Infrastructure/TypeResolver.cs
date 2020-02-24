namespace Newsgirl.WebServices.Infrastructure
{
    using System;

    using Autofac;

    /// <summary>
    /// Wraps around the IoC interface. Resolves instances on demand.
    /// </summary>
    public class TypeResolver
    {
        private ILifetimeScope Scope { get; }

        public TypeResolver(ILifetimeScope scope)
        {
            this.Scope = scope;
        }

        public T Resolve<T>()
        {
            return this.Scope.Resolve<T>();
        }

        public object Resolve(Type type)
        {
            return this.Scope.Resolve(type);
        }
    }
}