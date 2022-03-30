using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.Tests
{
    [TestClass]
    public class HyperLogLog14Test
    {
        [TestMethod]
        public void ForecastTest10W()
        {
            var size = 100_000;
            byte[] bytes = new byte[size * 8];
            Random.Shared.NextBytes(bytes);
            var sum = 0F;
            for (int n = 0; n < 100; n++)
            {
                Random.Shared.NextBytes(bytes);
                var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
                using var hyper = new HyperLogLog14();
                for (int i = 0; i < size; i++)
                    hyper.Insert(rs[i]);
                var count = hyper.Count();
                //var count = HyperLogLog.Count14(rs);
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
