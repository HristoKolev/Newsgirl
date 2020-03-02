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
        
        [Fact]
        public void Stream_Overload_Returns_Correct_Result()
        {
            var hasher = new Hasher();

            var bytes = Enumerable.Range(0, 8).Select(x => (byte)x).ToArray();

            using (var memoryStream = new MemoryStream(bytes))
            {
                long value = hasher.ComputeHash(memoryStream);
            
                Assert.Equal(-8626056615231480947, value);   
            }
        }
    }
}