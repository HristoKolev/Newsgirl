using System.Collections.Concurrent;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace Newsgirl.Shared.Tests
{
    public class ObjectPoolTest
    {
        [Fact]
        public async Task Get_Reuses_Instances()
        {
            const int threadCount = 10;
            const int workItemCount = threadCount * 10;
            
            ThreadPool.SetMinThreads(threadCount, 10);
            ThreadPool.SetMaxThreads(threadCount, 10);
            
            var pool = new ObjectPool<PoolTestObject>(() => Task.FromResult(new PoolTestObject()));

            var set = new ConcurrentDictionary<PoolTestObject, int>();

            var tasks = Enumerable.Range(0, workItemCount)
                .Select(_ => Task.Run(async () =>
                {
                    using (var wrapper = await pool.Get())
                    {
                        set.TryAdd(wrapper.Instance, 0);
                    }
                })).ToList();

            await Task.WhenAll(tasks);
            
            Assert.InRange(set.Count, 1, threadCount);
        }
    }

    public class PoolTestObject
    {
    }
}
