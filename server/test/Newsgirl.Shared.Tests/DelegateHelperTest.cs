namespace Newsgirl.Shared.Tests
{
    using System;
    using System.Threading.Tasks;
    using Xunit;

    public class DelegateHelperTest
    {
        [Fact]
        public async Task DebounceWorks()
        {
            int i = 0;
            var duration = TimeSpan.FromMilliseconds(100);

            var run = DelegateHelper.Debounce(() => i++, duration);

            for (int j = 0; j < 10; j++)
            {
                run();
                await Task.Delay(1);
            }

            await Task.Delay(duration.Add(TimeSpan.FromMilliseconds(20)));

            for (int j = 0; j < 10; j++)
            {
                run();
                await Task.Delay(1);
            }

            Assert.InRange(i, 1, 5);
        }
    }
}
