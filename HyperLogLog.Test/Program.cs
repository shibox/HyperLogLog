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
            IHyperLogLog<uint> estimator = new FastHyperLogLog();
            uint[] array = new uint[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (uint)i;
            Stopwatch w = Stopwatch.StartNew();
            
            estimator.BulkAdd(array);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine(count + "    cost:" + w.ElapsedMilliseconds);
            Console.ReadLine();
        }

        private static void TestCountString()
        {
            IHyperLogLog<string> estimator = new FastHyperLogLog(14);
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
