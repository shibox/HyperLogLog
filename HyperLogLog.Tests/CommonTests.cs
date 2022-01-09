using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace HyperLogLog.Tests
{
    [TestClass]
    public class CommonTests
    {
        private Stopwatch stopwatch;

        [TestInitialize]
        public void Init()
        {
            this.stopwatch = new Stopwatch();
            this.stopwatch.Start();
        }

        [TestCleanup]
        public void Cleanup()
        {
            this.stopwatch.Stop();
            Console.WriteLine("Total test time: {0}", this.stopwatch.Elapsed);
        }

        //[TestMethod]
        //public void TestGetSigma()
        //{
        //    // simulate a 64 bit hash and 14 bits for indexing
        //    const int bitsToCount = 64 - 14;
        //    Assert.AreEqual(51, Utils.GetSigma(0, bitsToCount));
        //    Assert.AreEqual(50, HyperLogLog.GetSigma(1, bitsToCount));
        //    Assert.AreEqual(47, HyperLogLog.GetSigma(8, bitsToCount));
        //    Assert.AreEqual(1, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount) - 1), bitsToCount));
        //    Assert.AreEqual(51, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount + 1)), bitsToCount));
        //}

        /// <summary>
        /// 验证多种GetSigma实现的正确性
        /// </summary>
        [TestMethod]
        public void CheckSigmaTest()
        {
            var bytes = new byte[80_000_000];
            for (int n = 0; n < 10; n++)
            {
                Random.Shared.NextBytes(bytes);
                var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
                CheckSigma(rs);
            }
        }
        
        private static void CheckSigma(Span<ulong> values)
        {
            int error = 0;
            byte[] mask = Utils.InitMask(9);
            for (int i = 0; i < values.Length; i++)
            {
                ulong hash = values[i];
                int sigmaA = Utils.GetSigmaLookup(hash, mask);
                int sigmaB = Utils.GetSigmaCommon(hash);
                int sigmaC = Utils.GetSigmaLeading(hash);
                if (sigmaA != sigmaB || sigmaA != sigmaC)
                    error++;
            }
            Assert.AreEqual(0, error);
        }

    }
}

