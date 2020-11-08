namespace Newsgirl.Shared.Tests
{
    using Xunit;

    public class RngServiceImplTest
    {
        [Theory]
        [InlineData(0)]
        [InlineData(1)]
        [InlineData(10)]
        [InlineData(100)]
        public void GeneratesStringsOfCorrectLength(int length)
        {
            var rng = new RngServiceImpl();
            string result = rng.GenerateSecureString(length);
            Assert.Equal(length, result.Length);
        }
    }
}
