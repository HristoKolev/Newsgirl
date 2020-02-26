using System;
using System.Threading;
using System.Threading.Tasks;

namespace Newsgirl.Shared.Infrastructure
{
    /// <summary>
    /// Provides asynchronous locking functionality without any extras.
    /// Uses `SemaphoreSlim` under the hood.
    /// To use create a new instance and call the `await Lock()` method in a using statement.
    /// </summary>
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

    /// <summary>
    /// The object that gets returned by the AsyncLock's Lock() method.
    /// The only point of this class is to implement the `IDisposable` interface,
    /// releasing the lock on Disposing.  
    /// </summary>
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