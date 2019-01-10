namespace Newsgirl.WebServices.Infrastructure
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    public class AsyncLock
    {
        private readonly SemaphoreSlim semaphore;

        public AsyncLock()
        {
            this.semaphore = new SemaphoreSlim(1, 1);
        }

        public async Task<AsyncLockInstance> Lock()
        {
            await this.semaphore.WaitAsync();
            return new AsyncLockInstance(this.semaphore);
        }
    }

    public class AsyncLockInstance : IDisposable
    {
        private readonly SemaphoreSlim semaphore;

        public AsyncLockInstance(SemaphoreSlim semaphore)
        {
            this.semaphore = semaphore;
        }

        public void Dispose()
        {
            this.semaphore.Release();
        }
    }
}