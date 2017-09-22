﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.Performance.Tests
{
    public class HyperLogLogTests
    {
        public static void Run()
        {
            //TestCountInt32();
            //TestCountUInt32();
            //TestCountString();
            //TestCountUInt64();
            //TestCountInt32AsByte();
            //TestCheckSigma();
            //TestBench();
            TestValidity();
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
            int n = 0;
            Stopwatch w = Stopwatch.StartNew();
            for (int i = 0; i < 100; i++)
                estimator.BulkAdd(array, 0, array.Length);
            //n = HyperLogLog.Count(array, 0, array.Length);
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
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + (((double)1.0 - ((double)array.Length / (double)count)) * 100).ToString("f8"));
            Console.ReadLine();
        }

        private static void TestCheckSigma()
        {
            HyperLogLog.InitMask(9);
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            for (int n = 0; n < 100; n++)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = rd.Next();
                HyperLogLog.Hash(array, 0, array.Length, rs);
                HyperLogLog.CheckSigma(rs, 0, rs.Length);
            }
        }

        private static void TestBench()
        {
            int count = 0;
            HyperLogLog.InitMask(9);
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            Dictionary<int, bool> map = new Dictionary<int, bool>(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = rd.Next();
                if (map.ContainsKey(array[i]) == false)
                    map.Add(array[i], true);
            }
                
            HyperLogLog.Hash(array, 0, array.Length, rs);
            Stopwatch w = Stopwatch.StartNew();
            //for (int n = 0; n < 100; n++)
            //{
            //    HyperLogLog.CountCompute(rs, 0, rs.Length);
            //}
            count = (int)HyperLogLog.CountCompute(rs, 0, rs.Length);
            w.Stop();
            Console.WriteLine("real count:" + map.Count + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (map.Count / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();

        }

        private static void TestValidity()
        {
            int countA = 0,countB=0;
            HyperLogLog.InitMask(9);
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            Dictionary<int, bool> map = new Dictionary<int, bool>(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = rd.Next();
                if (map.ContainsKey(array[i]) == false)
                    map.Add(array[i], true);
            }

            HyperLogLog.Hash(array, 0, array.Length, rs);
            Stopwatch w = Stopwatch.StartNew();
            countA = (int)HyperLogLog.CountCompute(rs, 0, rs.Length);

            IHyperLogLog<int> estimator = new HyperLogLog();
            estimator.BulkAdd(array, 0, array.Length);
            countB = (int)estimator.Count();
            
            Console.WriteLine("real count:" + map.Count + " estimator count:" + countA + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (map.Count / (float)countA)) * 100).ToString("f4"));
            Console.WriteLine($"{countA == countB}  countA:{countA}   countB:{countB}");
            Console.ReadLine();

        }


    }
}
