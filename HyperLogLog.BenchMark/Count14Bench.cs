using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.BenchMark
{
    /// <summary>
    /// 通过使用硬件指令LeadingZeroCount实现的Count14性能是最好的
    /// 
    /// |            Method |  N |      Mean | Error | Ratio | Rank |
    /// |------------------ |--- |----------:|------:|------:|-----:|
    /// |     Count14Common | 10 | 828.36 ms |    NA |  9.49 |    4 |
    /// |       Count14Simd | 10 | 107.95 ms |    NA |  1.24 |    2 |
    /// |     Count14Lookup | 10 | 115.41 ms |    NA |  1.32 |    3 |
    /// |           Count14 | 10 |  87.30 ms |    NA |  1.00 |    1 |
    /// </summary>
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, targetCount: 1)]
    [RankColumn]
    public class Count14Bench
    {
        static byte[] bytes = new byte[80_000_000];

        [Params(10)]
        public int N;

        [GlobalSetup]
        public void Setup()
        {
            Random.Shared.NextBytes(bytes);
        }

        [Benchmark]
        public unsafe void Count14Common()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                HyperLogLog.Count14UseCommon(rs);
        }

        [Benchmark]
        public unsafe void Count14Simd()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                HyperLogLog.Count14UseSimd(rs);
        }

        [Benchmark]
        public unsafe void Count14Lookup()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                HyperLogLog.Count14UseLookup(rs);
        }

        [Benchmark(Baseline = true)]
        public unsafe void Count14()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                HyperLogLog.Count14(rs);
        }


    }

}
