using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

namespace Newsgirl.Shared.Tests
{
    using System.Runtime.Serialization;

    public class AsyncLockTest
    {
        [Fact]
        public async Task Does_Not_Allow_Concurrent_Access()
        {
            RaceConditionException expectedError = null;
            
            try
            {
                await RunConcurrentTest(async action =>
                {
                    await action();
                });
            }
            catch (RaceConditionException err)
            {
                expectedError = err;
            }

            if (expectedError == null)
            {
                throw new ApplicationException("Race condition did not occur when expected.");
            }
            
            var asyncLock = new AsyncLock();

            await RunConcurrentTest(async action =>
            {
                using (await asyncLock.Lock())
                {
                    await action();
                }
            });
        }

        private static async Task RunConcurrentTest(Func<Func<Task>, Task> wrapperFunc)
        {
            const int THREAD_COUNT = 2;
            
            bool open = false;

            var tasks = Enumerable.Range(0, THREAD_COUNT)
                .Select(_ => Task.Run(async () =>
                {
                    await wrapperFunc(async () =>
                    {
                        if (open)
                        {
                            throw new RaceConditionException();
                        }

                        open = true;

                        await Task.Delay(100);

                        open = false;
                    });
                }));

            await Task.WhenAll(tasks);
        }

        private class RaceConditionException : Exception
        {
            public RaceConditionException()
            {
            }

            public RaceConditionException(string message) : base(message)
            {
            }

            public RaceConditionException(string message, Exception inner) : base(message, inner)
            {
            }

            protected RaceConditionException(
                SerializationInfo info,
                StreamingContext context) : base(info, context)
            {
            }
        }
    }
}
