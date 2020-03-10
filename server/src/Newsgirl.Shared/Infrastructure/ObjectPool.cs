using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace Newsgirl.Shared.Infrastructure
{
    /// <summary>
    /// A naive object pool implementation with no limitation for number of created instances
    /// and very basic concurrency support.
    /// </summary>
    public class ObjectPool<T> where T : class
    {
        private readonly Func<Task<T>> factory;

        /// <summary>
        /// Used for storage for available instances.
        /// </summary>
        private readonly ConcurrentQueue<T> queue = new ConcurrentQueue<T>();

        /// <summary>
        /// Takes an async factory method that gets called in order to create a new instance. 
        /// </summary>
        public ObjectPool(Func<Task<T>> factory)
        {
            this.factory = factory;
        }
        
        /// <summary>
        /// Creates an instance wrapper type that needs to be disposed
        /// for the instance to get back on the pool. Creates a new instance if there is none available.
        /// </summary>
        public async Task<ObjectPoolInstanceWrapper<T>> Get()
        {
            T instance;
            
            if (!this.queue.TryDequeue(out instance))
            {
                instance = await this.factory();   
            }
            
            return new ObjectPoolInstanceWrapper<T>(instance, this.queue);
        }
    }

    /// <summary>
    /// Holds an instance and adds it back to the pool on disposal.
    /// </summary>
    public class ObjectPoolInstanceWrapper<T> : IDisposable where T : class
    {
        private ConcurrentQueue<T> queue;

        public T Instance { get; private set; }

        public ObjectPoolInstanceWrapper(T instance, ConcurrentQueue<T> queue)
        {
            this.Instance = instance;
            this.queue = queue;
        }

        public void Dispose()
        {
            this.queue.Enqueue(this.Instance);
            this.queue = null;
            this.Instance = null;
        }
    }
}
