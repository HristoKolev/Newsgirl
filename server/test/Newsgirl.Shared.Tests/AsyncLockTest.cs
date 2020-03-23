using System;
using System.Linq;
using System.Threading.Tasks;

using Xunit;

using Newsgirl.Shared.Infrastructure;

namespace Newsgirl.Shared.Tests
{
    public class AsyncLockTest
    {
        [Fact]
        public async Task Does_Not_Allow_Concurrent_Access()
        {
            string expectedString;
            string actualString;
            
            (expectedString, actualString) = await RunConcurrentTest(async action =>
            {
                await action();
            });

            Assert.NotEqual(expectedString, actualString);

            var asyncLock = new AsyncLock();

            (expectedString, actualString) = await RunConcurrentTest(async action =>
            {
                using (await asyncLock.Lock())
                {
                    await action();
                }
            });
            
            Assert.Equal(expectedString, actualString);
        }

        private static async Task<(string, string)> RunConcurrentTest(Func<Func<Task>, Task> wrapperFunc)
        {
            const int bufferSize = 50;
            const int iterationCount = 2;

            int[] buffer = new int[bufferSize];

            var tasks = Enumerable.Range(0, iterationCount)
                .Select(_ => Task.Run(async () =>
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var index = i;
                        
                        await wrapperFunc(async () =>
                        {
                            await Task.Delay(1);
                            
                            buffer[index] = buffer[index] + 1;
                        });
                    }
                }));

            await Task.WhenAll(tasks);

            int[] expected = Enumerable.Repeat(iterationCount, bufferSize).Select(x => x).ToArray();

            string expectedString = string.Join(", ", expected.Select(x => x.ToString()));
            string actualString = string.Join(", ", buffer.Select(x => x.ToString()));

            return (expectedString, actualString);
        }
    }
}