using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.Tests
{
    /// <summary>
    /// 测试平均误差率0.6%左右
    /// </summary>
    [TestClass]
    public class AccuracyTest
    {
        [TestMethod]
        public void ForecastTest1K()
        {
            ForecastTest(1_000);
        }

        [TestMethod]
        public void ForecastTest1W()
        {
            ForecastTest(10_000);
        }

        [TestMethod]
        public void ForecastTest2W()
        {
            ForecastTest(20_000);
        }

        [TestMethod]
        public void ForecastTest5W()
        {
            ForecastTest(50_000);
        }

        [TestMethod]
        public void ForecastTest10W()
        {
            ForecastTest(100_000);
        }

        private static void ForecastTest(int size)
        {
            byte[] bytes = new byte[size * 8];
            var sum = 0F;
            for (int n = 0; n < 100; n++)
            {
                Random.Shared.NextBytes(bytes);
                var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
                var count = HyperLogLog.Count14(rs);
                var error = (float)count - size;
                var rate = error / size * 100;
                sum += Math.Abs(error);
                Console.WriteLine($"error rate:{rate.ToString("f2")} %,count is:{count}");
            }
            var avg = (float)sum / (size * 100) * 100;
            Console.WriteLine($"avg error rate:{avg.ToString("f2")} %");
        }
    }
}
