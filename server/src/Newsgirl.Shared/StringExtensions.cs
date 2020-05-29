namespace Newsgirl.Shared
{
    public static class StringExtensions
    {
        /// <summary>
        ///     I use this to prevent empty strings from being stored in the database.
        /// </summary>
        public static string SomethingOrNull(this string x)
        {
            return string.IsNullOrWhiteSpace(x) ? null : x;
        }
    }
}
