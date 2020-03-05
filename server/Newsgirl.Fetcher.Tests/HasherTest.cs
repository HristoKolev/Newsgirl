using System.IO;
using System.Linq;
using Xunit;

namespace Newsgirl.Fetcher.Tests
{
    public class HasherTest
    {
        [Fact]
        public void Bytes_Overload_Returns_Correct_Result()
        {
            var hasher = new Hasher();

            var bytes = Enumerable.Range(0, 8).Select(x => (byte)x).ToArray();

            long value = hasher.ComputeHash(bytes);
            
            Assert.Equal(-8626056615231480947, value);
        }
    }
}