namespace Newsgirl.Shared.Tests
{
    using System.Collections.Concurrent;
    using System.Linq;
    using System.Threading;
    using System.Threading.Tasks;
    using Xunit;

    public class ObjectPoolTest
    {
        [Fact]
        public async Task Get_Reuses_Instances()
        {
            const int THREAD_COUNT = 10;
            const int WORK_ITEM_COUNT = THREAD_COUNT * 10;

            ThreadPool.SetMinThreads(THREAD_COUNT, 10);
            ThreadPool.SetMaxThreads(THREAD_COUNT, 10);

            var pool = new ObjectPool<PoolTestObject>(() => Task.FromResult(new PoolTestObject()));

            var set = new ConcurrentDictionary<PoolTestObject, int>();

            var tasks = Enumerable.Range(0, WORK_ITEM_COUNT)
                .Select(_ => Task.Run(async () =>
                {
                    using (var wrapper = await pool.Get())
                    {
                        set.TryAdd(wrapper.Instance, 0);
                    }
                })).ToList();

            await Task.WhenAll(tasks);

            Assert.InRange(set.Count, 1, THREAD_COUNT);
        }
    }

    public class PoolTestObject { }
}
