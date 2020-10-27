namespace Newsgirl.Shared
{
    using System;
    using Microsoft.Extensions.ObjectPool;

    /// <summary>
    /// Creates objects by invoking the factory function. Always returns objects to the pool.
    /// </summary>
    public class FunctionFactoryObjectPolicy<T> : DefaultPooledObjectPolicy<T> where T : class, new()
    {
        private readonly Func<T> func;

        public FunctionFactoryObjectPolicy(Func<T> func)
        {
            this.func = func;
        }

        public override T Create()
        {
            return this.func();
        }
    }
}
