﻿using System;
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
            //TestCountInt32();
            //TestCountUInt32();
            //TestCountString();
            //TestCountUInt64();
            TestCountInt32AsByte();
        }

        private static void TestCountUInt32()
        {
            IHyperLogLog<uint> estimator = new HyperLogLog();
            uint[] array = new uint[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (uint)i;
            Stopwatch w = Stopwatch.StartNew();
            //estimator.BulkAdd(array,0,array.Length);
            for (int i = 0; i < array.Length; i++)
                estimator.Add(array[i]);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (array.Length / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();
        }

        private static void TestCountInt32()
        {
            IHyperLogLog<int> estimator = new HyperLogLog();
            int[] array = new int[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;
            Stopwatch w = Stopwatch.StartNew();
            estimator.BulkAdd(array, 0, array.Length);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (array.Length / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();
        }

        private static void TestCountUInt64()
        {
            IHyperLogLog<ulong> estimator = new HyperLogLog();
            ulong[] array = new ulong[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (ulong)i;
            Stopwatch w = Stopwatch.StartNew();
            estimator.BulkAdd(array, 0, array.Length);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (array.Length / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();
        }

        private static void TestCountString()
        {
            IHyperLogLog<string> estimator = new HyperLogLog();
            Stopwatch w = Stopwatch.StartNew();
            estimator.Add("Alice");
            estimator.Add("Bob");
            estimator.Add("Alice");
            estimator.Add("George Michael");
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + 4 + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (4 / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();
            
        }

        private static void TestCountInt32AsByte()
        {
            IHyperLogLog<int> estimator = new HyperLogLog();
            int[] array = new int[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;
            byte[] bytes = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            Stopwatch w = Stopwatch.StartNew();
            estimator.AddAsInt(bytes, 0, array.Length);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length +  " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + (((double)1.0 - ((double)array.Length / (double)count)) * 100).ToString("f8"));
            Console.ReadLine();
        }

    }
}
