using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Diagnostics;

namespace HyperLogLog.Tests
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {

        }

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

        [TestMethod]
        public void TestGetSigma()
        {
            // simulate a 64 bit hash and 14 bits for indexing
            const int bitsToCount = 64 - 14;
            //Assert.AreEqual(51, Utils.GetSigma(0, bitsToCount));
            //Assert.AreEqual(50, HyperLogLog.GetSigma(1, bitsToCount));
            //Assert.AreEqual(47, HyperLogLog.GetSigma(8, bitsToCount));
            //Assert.AreEqual(1, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount) - 1), bitsToCount));
            //Assert.AreEqual(51, HyperLogLog.GetSigma((ulong)(Math.Pow(2, bitsToCount + 1)), bitsToCount));
        }

        [TestMethod]
        private void TestCheckSigma()
        {
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            var rd = new Random();
            for (int n = 0; n < 100; n++)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = rd.Next();
                Utils.Hash(array, 0, array.Length, rs);
                CheckSigma(rs, 0, rs.Length);
            }
        }
        
        public void CheckSigma(ulong[] values, int offset, int size)
        {
            int error = 0;
            byte[] mask = Utils.InitMask(9);
            for (uint i = 0; i < size; i++)
            {
                ulong hash = values[i + offset];
                int sigmaA = Utils.GetSigma(hash, mask);
                int sigmaB = Utils.GetSigma(hash);
                int sigmaC = Utils.GetSigmaLeading(hash);
                if (sigmaA != sigmaB || sigmaA != sigmaC)
                    error++;
            }
            Assert.AreEqual(0, error);
            Console.WriteLine($"error:{error}");
        }

    }
}

