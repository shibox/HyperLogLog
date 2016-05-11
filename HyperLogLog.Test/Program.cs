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
            IHyperLogLog<int> estimator = new CardinalityEstimator(14);

            Stopwatch w = Stopwatch.StartNew();
            for (int i = 1; i <= 1000000; i++)
                estimator.Add(i);
            //estimator.Add("Alice");
            //estimator.Add("Bob");
            //estimator.Add("Alice");
            //estimator.Add("George Michael");
            w.Stop();

            ulong numberOfuniqueElements = estimator.Count(); // will be 3
            Console.WriteLine(numberOfuniqueElements + "    cost:" + w.ElapsedMilliseconds);
            Console.ReadLine();
        }
    }
}
