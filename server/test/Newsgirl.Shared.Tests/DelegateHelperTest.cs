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
            var duration = TimeSpan.FromMilliseconds(20);
            var delta = TimeSpan.FromMilliseconds(5);

            var run = DelegateHelper.Debounce(() => i++, duration);

            for (int j = 0; j < 10; j++)
            {
                run();
                await Task.Delay(duration.Subtract(delta));
            }

            await Task.Delay(duration.Add(delta));

            for (int j = 0; j < 10; j++)
            {
                run();
                await Task.Delay(duration.Subtract(delta));
            }

            Assert.Equal(3, i);
        }
    }
}
