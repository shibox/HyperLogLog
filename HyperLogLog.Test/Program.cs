using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.Test
{
    class Program
    {
        static void Main(string[] args)
        {
            TestCountInt32();
            //TestCountString();
        }

        private static void TestCountInt32()
        {
            IHyperLogLog<int> estimator = new CardinalityEstimator(14);

            Stopwatch w = Stopwatch.StartNew();
            for (int i = 1; i <= 1000000; i++)
                estimator.Add(i);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine(count + "    cost:" + w.ElapsedMilliseconds);
            Console.ReadLine();
        }

        private static void TestCountString()
        {
            IHyperLogLog<string> estimator = new CardinalityEstimator(14);
            Stopwatch w = Stopwatch.StartNew();
            estimator.Add("Alice");
            estimator.Add("Bob");
            estimator.Add("Alice");
            estimator.Add("George Michael");
            w.Stop();

            ulong count = estimator.Count(); // will be 3
            Console.WriteLine(count + "    cost:" + w.ElapsedMilliseconds);
            Console.ReadLine();
            
        }

    }
}
