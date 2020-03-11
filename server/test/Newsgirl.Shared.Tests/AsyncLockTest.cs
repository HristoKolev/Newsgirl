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
                action();
            });

            Assert.NotEqual(expectedString, actualString);

            var asyncLock = new AsyncLock();
            
            (expectedString, actualString) = await RunConcurrentTest(async action =>
            {
                using (await asyncLock.Lock())
                {
                    action();
                }
            });
            
            Assert.Equal(expectedString, actualString);
        }

        private static async Task<(string, string)> RunConcurrentTest(Func<Action, Task> wrapperFunc)
        {
            const int bufferSize = 1000;
            const int iterationCount = 100;

            int[] buffer = new int[bufferSize];

            var tasks = Enumerable.Range(0, iterationCount)
                .Select(_ => Task.Run(async () =>
                {
                    for (int i = 0; i < buffer.Length; i++)
                    {
                        var index = i;
                        
                        await wrapperFunc(() => { buffer[index] = buffer[index] + 1; });
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