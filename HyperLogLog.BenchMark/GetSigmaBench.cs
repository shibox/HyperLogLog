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
    /// 优化后的使用硬件指令的性能是最原始的标量方法的8倍
    /// 
    /// BenchmarkDotNet=v0.13.1, OS=Windows 10.0.19042.1165 (20H2/October2020Update)
    /// 11th Gen Intel Core i7-11370H 3.30GHz, 1 CPU, 8 logical and 4 physical cores
    /// .NET SDK=6.0.100
    ///   [Host]     : .NET 6.0.0 (6.0.21.37719), X64 RyuJIT
    ///   Job-PMHNPU : .NET 6.0.0 (6.0.21.37719), X64 RyuJIT
    /// 
    /// IterationCount=1  LaunchCount=1  RunStrategy=ColdStart
    /// WarmupCount=1
    /// 
    /// |            Method |  N |      Mean | Error | Ratio | Rank |
    /// |------------------ |--- |----------:|------:|------:|-----:|
    /// |  CountSigmaCommon | 10 | 857.89 ms |    NA |  9.07 |    3 |
    /// |  CountSigmaLookup | 10 | 121.52 ms |    NA |  1.28 |    2 |
    /// | CountSigmaLeading | 10 |  94.61 ms |    NA |  1.00 |    1 |
    /// </summary>
    [SimpleJob(RunStrategy.ColdStart, launchCount: 1, warmupCount: 1, targetCount: 1)]
    [RankColumn]
    public class GetSigmaBench
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
        public unsafe void CountSigmaCommon()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                CountSigmaCommon(rs);
        }

        [Benchmark]
        public unsafe void CountSigmaLookup()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                CountSigmaLookup(rs);
        }

        [Benchmark(Baseline = true)]
        public unsafe void CountSigmaLeading()
        {
            var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
            for (int i = 0; i < N; i++)
                CountSigmaLeadingFast(rs);
        }

        public unsafe static void CountSigmaCommon(Span<ulong> values)
        {
            long v = 0;
            byte[] mask = Utils.InitMask(9);
            fixed (ulong* pd = values)
            {
                ulong* start = pd;
                ulong* end = pd + values.Length;
                while (start < end)
                {
                    v += Utils.GetSigmaCommon(*start);
                    start++;
                }
            }
        }

        public unsafe static void CountSigmaLookup(Span<ulong> values)
        {
            long v = 0;
            byte[] mask = Utils.InitMask(9);
            fixed (ulong* pd = values)
            {
                ulong* start = pd;
                ulong* end = pd + values.Length;
                while (start < end)
                {
                    v += Utils.GetSigmaLookup(*start, mask);
                    start++;
                }
            }
        }

        public unsafe static void CountSigmaLeadingFast(Span<ulong> values)
        {
            long v = 0;
            fixed (ulong* pd = values)
            {
                ulong* start = pd;
                ulong* end = pd + values.Length;
                while (start < end)
                {
                    v += Utils.GetSigmaLeading(*start);
                    start++;
                }
            }
        }



    }
}
