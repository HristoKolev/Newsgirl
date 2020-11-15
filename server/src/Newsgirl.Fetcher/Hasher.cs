namespace Newsgirl.Fetcher
{
    using Standart.Hash.xxHash;

    public class Hasher
    {
        public long ComputeHash(byte[] bytes)
        {
            return (long) xxHash64.ComputeHash(bytes);
        }
    }
}
