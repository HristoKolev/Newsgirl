namespace Newsgirl.WebServices.Infrastructure
{
    public static class StringExtensions
    {
        public static string SomethingOrNull(this string x)
        {
            return string.IsNullOrWhiteSpace(x) ? null : x;
        }
    }
}