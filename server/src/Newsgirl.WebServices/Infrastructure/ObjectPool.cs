namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class ObjectPool<T> where T : class
    {
        private readonly Func<Task<T>> factory;

        private ConcurrentQueue<T> Queue { get; } = new ConcurrentQueue<T>();

        public ObjectPool(Func<Task<T>> factory)
        {
            this.factory = factory;
        }
        
        public async Task<ObjectPoolInstanceWrapper<T>> Get()
        {
            T instance;
            
            if (!this.Queue.TryDequeue(out instance))
            {
                instance = await this.factory();   
            }
            
            return new ObjectPoolInstanceWrapper<T>(instance, this.Queue);
        }
    }

    public class ObjectPoolInstanceWrapper<T> : IDisposable where T : class
    {
        public ObjectPoolInstanceWrapper(T instance, ConcurrentQueue<T> queue)
        {
            this.Instance = instance;
            this.Queue = queue;
        }

        private ConcurrentQueue<T> Queue { get; }

        public T Instance { get; set; }

        public void Dispose()
        {
            this.Queue.Enqueue(this.Instance);
            this.Instance = null;
        }
    }
}