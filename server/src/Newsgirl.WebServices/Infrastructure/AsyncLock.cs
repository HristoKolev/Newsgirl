namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncLock : IDisposable
    {
        private SemaphoreSlim Semaphore { get; }

        private AsyncLock(SemaphoreSlim semaphore)
        {
            this.Semaphore = semaphore;
        }

        public static async Task<AsyncLock> Create(SemaphoreSlim semaphore)
        {
            var instance = new AsyncLock(semaphore);
            await instance.Semaphore.WaitAsync();
            return instance;
        }

        public void Dispose()
        {
            this.Semaphore.Release();
        }
    }
}