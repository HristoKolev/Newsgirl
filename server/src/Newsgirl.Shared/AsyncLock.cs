namespace Newsgirl.Shared
{
    using System;
    using System.Threading;
    using System.Threading.Tasks;

    /// <summary>
    /// Provides asynchronous locking functionality without any extras.
    /// Uses `SemaphoreSlim` under the hood.
    /// To use create a new instance and call the `await Lock()` method in a using statement.
    /// </summary>
    public class AsyncLock
    {
        private readonly LockDisposer lockDisposer;
        private readonly SemaphoreSlim semaphore;

        public AsyncLock()
        {
            this.semaphore = new SemaphoreSlim(1, 1);

            this.lockDisposer = new LockDisposer(this.semaphore);
        }

        public async ValueTask<IDisposable> Lock()
        {
            await this.semaphore.WaitAsync();

            return this.lockDisposer;
        }

        private class LockDisposer : IDisposable
        {
            private readonly SemaphoreSlim semaphore;

            public LockDisposer(SemaphoreSlim semaphore)
            {
                this.semaphore = semaphore;
            }

            public void Dispose()
            {
                this.semaphore.Release();
            }
        }
    }
}
