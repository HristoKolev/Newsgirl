using System;
using System.Data.HashFunction.xxHash;
using System.IO;

namespace Newsgirl.Fetcher
{
    public class Hasher : IHasher
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

        public long ComputeHash(Stream stream)
        {
            byte[] hashBytes = this.xxHash.ComputeHash(stream).Hash;

            long value = BitConverter.ToInt64(hashBytes);

            return value;
        }
    }

    public interface IHasher
    {
        long ComputeHash(byte[] bytes);
        
        long ComputeHash(Stream stream);
    }
}