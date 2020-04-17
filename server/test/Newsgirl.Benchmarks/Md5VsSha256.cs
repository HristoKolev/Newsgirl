using System;
using System.Security.Cryptography;
using BenchmarkDotNet.Attributes;

namespace Newsgirl.Benchmarks
{
    [MemoryDiagnoser]
    public class Md5VsSha256
    {
        private const int N = 10000;
        private readonly byte[] data;

        private readonly SHA256 sha256 = SHA256.Create();
        private readonly MD5 md5 = MD5.Create();

        public Md5VsSha256()
        {
            this.data = new byte[N];
            new Random(42).NextBytes(this.data);
        }

        [Benchmark]
        public byte[] Sha256() => this.sha256.ComputeHash(this.data);

        [Benchmark]
        public byte[] Md5() => this.md5.ComputeHash(this.data);
    }
}