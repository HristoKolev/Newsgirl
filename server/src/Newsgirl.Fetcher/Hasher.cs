namespace Newsgirl.Fetcher
{
    using System;
    using System.Data.HashFunction.xxHash;

    public class Hasher
    {
        private readonly IxxHash xxHash;

        public Hasher()
        {
            this.xxHash = xxHashFactory.Instance.Create(new xxHashConfig
            {
                HashSizeInBits = 64,
            });
        }

        public long ComputeHash(byte[] bytes)
        {
            var hashBytes = this.xxHash.ComputeHash(bytes).Hash;

            long value = BitConverter.ToInt64(hashBytes);

            return value;
        }
    }
}
