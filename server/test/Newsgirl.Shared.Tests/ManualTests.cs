namespace Newsgirl.Shared.Tests
{
    using System.Linq;
    using Xunit;

    public class ManualTests : TestPocosDatabaseTest
    {
        [Fact]
        public void FunctionsWork()
        {
            var query = from d in this.Db.Poco.VGenerateSeries
                select TestDbPocos.IncrementByOne(d.Num) == d.Num + 1;

            bool result = query.FirstOrDefault();

            Assert.True(result);
        }
    }
}
