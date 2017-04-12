using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Diagnostics;

namespace HyperLogLog.Tests
{
    [TestClass]
    public class HyperLogLogTests
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

        [TestMethod]
        public void Test()
        {

        }

        //[TestMethod]
        //public void TestGetSigma()
        //{
        //    // simulate a 64 bit hash and 14 bits for indexing
        //    const int bitsToCount = 64 - 14;
        //    Assert.AreEqual(51, HyperLogLog.GetSigma(0, bitsToCount));
        //    Assert.AreEqual(50, HyperLogLog.GetSigma(1, bitsToCount));
        //    Assert.AreEqual(47, HyperLogLog.GetSigma(8, bitsToCount));
        //    Assert.AreEqual(1, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount) - 1), bitsToCount));
        //    Assert.AreEqual(51, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount + 1)), bitsToCount));
        //}
    }
}
