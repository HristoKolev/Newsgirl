namespace Newsgirl.Benchmarks
{
    using System;
    using System.Security.Cryptography;
    using BenchmarkDotNet.Attributes;

    [MemoryDiagnoser]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;
        private readonly MD5 md5 = MD5.Create();

        private readonly SHA256 sha256 = SHA256.Create();

        public Md5VsSha256()
        {
            this.data = new byte[N];
            new Random(42).NextBytes(this.data);
        }

        [Benchmark]
        public byte[] Sha256()
        {
            return this.sha256.ComputeHash(this.data);
        }

        [Benchmark]
        public byte[] Md5()
        {
            return this.md5.ComputeHash(this.data);
        }
    }
}
