namespace Newsgirl.Shared
{
    using System.Text;

    public static class EncodingHelper
    {
        /// <summary>
        /// An instance of UTF8Encoding that:
        /// * Does not add BOM when writing.
        /// * Throws when reading invalid input.
        /// </summary>
        // ReSharper disable once InconsistentNaming
        public static readonly UTF8Encoding UTF8 = new UTF8Encoding(false, true);
    }
}
