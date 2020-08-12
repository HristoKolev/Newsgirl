namespace Newsgirl.Shared.Tests
{
    using Xunit;

    public class ExtensionsTest
    {
        [Fact]
        public void SomethingOrNull_Returns_Correct_Result()
        {
            Assert.Null("".SomethingOrNull());
            Assert.Null("   ".SomethingOrNull());
            Assert.Null("\t".SomethingOrNull());
            Assert.Null("\t ".SomethingOrNull());
            Assert.Null("\n \n ".SomethingOrNull());
            Assert.Null("\r \r ".SomethingOrNull());

            Assert.Equal("123", "123".SomethingOrNull());
            Assert.Equal(" 123 ", " 123 ".SomethingOrNull());
        }
    }
}
