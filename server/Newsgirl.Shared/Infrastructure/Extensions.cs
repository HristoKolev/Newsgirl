namespace Newsgirl.Shared.Infrastructure
{
    public static class Extensions
    {
        /// <summary>
        /// I use this to prevent empty strings from being stored in the database.
        /// </summary>
        public static string SomethingOrNull(this string x)
        {
            return string.IsNullOrWhiteSpace(x) ? null : x;
        }
    }
}