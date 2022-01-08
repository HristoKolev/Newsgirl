namespace Newsgirl.Shared
{
    using System;
    using Standart.Hash.xxHash;

    public static class HashHelper
    {
        public static long ComputeXx64Hash(ReadOnlySpan<byte> data)
        {
            return (long)xxHash64.ComputeHash(data, data.Length);
        }
    }
}
