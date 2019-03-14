namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    public class ObjectPool<T> where T : class
    {
        private static Func<Task<T>> _factory;

        private ConcurrentQueue<T> Queue { get; } = new ConcurrentQueue<T>();
        
        public ObjectPool()
        {
            if (_factory == null)
            {
                throw new DetailedLogException("An ObjectPool factory is not configured for the requested type.")
                {
                    Context =
                    {
                        {
                            "type", typeof(T).FullName
                        }
                    }
                };
            }
        }

        public static void SetFactory(Func<Task<T>> factory)
        {
            _factory = factory;
        }

        public async Task<ObjectPoolInstanceWrapper<T>> Get()
        {
            T instance;
            
            if (!this.Queue.TryDequeue(out instance))
            {
                instance = await _factory();   
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