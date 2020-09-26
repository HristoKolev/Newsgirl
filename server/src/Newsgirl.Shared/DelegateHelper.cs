namespace Newsgirl.Shared
{
    using System;
    using System.Threading;

    public static class DelegateHelper
    {
        public static Action Debounce(Action x, TimeSpan duration)
        {
            var semaphore = new SemaphoreSlim(1, 1);
            Timer timer = null;
            bool fired = false;

            void RestartTimer()
            {
                timer?.Dispose();
                timer = new Timer(s =>
                {
                    semaphore.Wait();

                    try
                    {
                        timer!.Dispose();
                        timer = null;

                        if (fired)
                        {
                            x();
                            fired = false;
                        }
                    }
                    finally
                    {
                        semaphore.Release();
                    }
                }, null, duration, duration);
            }

            return () =>
            {
                semaphore.Wait();

                try
                {
                    if (timer == null)
                    {
                        x();
                        RestartTimer();
                    }
                    else
                    {
                        fired = true;
                        RestartTimer();
                    }
                }
                finally
                {
                    semaphore.Release();
                }
            };
        }
    }
}
