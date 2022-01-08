namespace Newsgirl.Shared.Tests
{
    using System.Linq;
    using Xunit;

    public class HashHelperTest
    {
        [Fact]
        public void Bytes_Overload_Returns_Correct_Result()
        {
            var bytes = Enumerable.Range(0, 8).Select(x => (byte)x).ToArray();

            long value = HashHelper.ComputeXx64Hash(bytes);

            Assert.Equal(-8626056615231480947, value);
        }
    }
}
