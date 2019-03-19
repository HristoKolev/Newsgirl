namespace Newsgirl.WebServices.Infrastructure
{
    using System;

    using StructureMap;

    /// <summary>
    /// Wraps around the IoC interface. Resolves instances on demand.
    /// </summary>
    public class TypeResolver
    {
        private IContainer Container { get; }

        public TypeResolver(IContainer container)
        {
            this.Container = container;
        }

        public T Resolve<T>()
        {
            return this.Container.GetInstance<T>();
        }

        public object Resolve(Type type)
        {
            return this.Container.GetInstance(type);
        }
    }
}