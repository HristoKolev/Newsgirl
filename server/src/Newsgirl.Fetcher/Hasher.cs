using System;
using System.Data.HashFunction.xxHash;

namespace Newsgirl.Fetcher
{
    public class Hasher
    {
        private readonly IxxHash xxHash;

        public Hasher()
        {
            this.xxHash = xxHashFactory.Instance.Create(new xxHashConfig
            {
                HashSizeInBits = 64
            });
        }

        public long ComputeHash(byte[] bytes)
        {
            byte[] hashBytes = this.xxHash.ComputeHash(bytes).Hash;

            long value = BitConverter.ToInt64(hashBytes);

            return value;
        }
    }
}