
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog.BenchMark
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
            //TestCheckSigmaBench();
            //TestBenchmark();
            //TestBenchmarkOp();
            TestBenchmarkOpFixed();
            //TestValidity();
            //TestHash();
        }

        private static void TestCountUInt32()
        {
            var estimator = new HyperLogLog();
            var array = new uint[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (uint)i;
            var w = Stopwatch.StartNew();
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
            var estimator = new HyperLogLog();
            var array = new int[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;
            var w = Stopwatch.StartNew();
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
            var estimator = new HyperLogLog();
            var array = new ulong[100000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = (ulong)i;
            var w = Stopwatch.StartNew();
            estimator.BulkAdd(array, 0, array.Length);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (array.Length / (float)count)) * 100).ToString("f4"));
            Console.ReadLine();
        }

        private static void TestCountString()
        {
            var estimator = new HyperLogLog();
            var w = Stopwatch.StartNew();
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
            var estimator = new HyperLogLog();
            var array = new int[10000000];
            for (int i = 0; i < array.Length; i++)
                array[i] = i;
            var bytes = new byte[array.Length * 4];
            Buffer.BlockCopy(array, 0, bytes, 0, bytes.Length);
            var w = Stopwatch.StartNew();
            estimator.AddAsInt(bytes, 0, array.Length);
            w.Stop();

            ulong count = estimator.Count();
            Console.WriteLine("real count:" + array.Length + " estimator count:" + count + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + (((double)1.0 - ((double)array.Length / (double)count)) * 100).ToString("f8"));
            Console.ReadLine();
        }

        private static void TestBenchmark()
        {
            int count = 0;
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            var rd = new Random(Guid.NewGuid().GetHashCode());
            for (int n = 0; n < 10000; n++)
            {
                var map = new Dictionary<int, bool>(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = rd.Next();
                    if (map.ContainsKey(array[i]) == false)
                        map.Add(array[i], true);
                }
                var w = Stopwatch.StartNew();
                Utils.Hash(array, 0, array.Length, rs);
                string s = $"hash cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(s);
                w = Stopwatch.StartNew();
                count = (int)HyperLogLog.Count14(rs);
                w.Stop();

                s = $"real count:{map.Count.ToString().PadLeft(8, ' ')} estimator count:{count.ToString().PadLeft(8, ' ')}  cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(s);

                s = $"error rate:{((1.0 - (map.Count / (float)count)) * 100).ToString("f4").PadLeft(7, ' ')}%";
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(s);
            }
        }

        private static void TestBenchmarkOp()
        {
            int count = 0;
            byte[] bytes = new byte[100_000_000 * 8];
            for (int n = 0; n < 10; n++)
            {
                Random.Shared.NextBytes(bytes);
                var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
                var w = Stopwatch.StartNew();
                count = (int)HyperLogLog.Count14(rs);
                w.Stop();

                var s = $"real count:{rs.Length.ToString().PadLeft(8, ' ')} estimator count:{count.ToString().PadLeft(8, ' ')}  cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(s);

                s = $"error rate:{((1.0 - (rs.Length / (float)count)) * 100).ToString("f4").PadLeft(7, ' ')}%";
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(s);
            }
            Console.ReadLine();

        }

        private static void TestBenchmarkOpFixed()
        {
            int count = 0;
            int tcost = 0;
            double trate = 0;
            byte[] bytes = new byte[100_000_000 * 8];
            for (int n = 0; n < 100; n++)
            {
                Random.Shared.NextBytes(bytes);
                var rs = MemoryMarshal.Cast<byte, ulong>(new Span<byte>(bytes));
                var w = Stopwatch.StartNew();
                count = (int)HyperLogLog.Count14(rs);
                w.Stop();
                tcost += (int)w.ElapsedMilliseconds;

                var s = $"real count:{rs.Length.ToString().PadLeft(8, ' ')} estimator count:{count.ToString().PadLeft(8, ' ')}  cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(s);

                var rate = ((1.0 - (rs.Length / (float)count)) * 100);
                trate += Math.Abs(rate);
                s = $"error rate:{rate.ToString("f4").PadLeft(7, ' ')}%";
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(s);
            }
            Console.WriteLine($"tcost:{tcost},trate:{trate / 100}");
            Console.ReadLine();

        }

        private static void TestValidity()
        {
            int countA = 0, countB = 0;
            var array = new int[10000000];
            var rs = new ulong[array.Length];
            var map = new Dictionary<int, bool>(array.Length);
            for (int i = 0; i < array.Length; i++)
            {
                array[i] = Random.Shared.Next();
                if (map.ContainsKey(array[i]) == false)
                    map.Add(array[i], true);
            }

            Utils.Hash(array, 0, array.Length, rs);
            var w = Stopwatch.StartNew();
            countA = (int)HyperLogLog.Count14(rs);

            var estimator = new HyperLogLog();
            estimator.BulkAdd(array, 0, array.Length);
            countB = (int)estimator.Count();
            
            Console.WriteLine("real count:" + map.Count + " estimator count:" + countA + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (map.Count / (float)countA)) * 100).ToString("f4"));
            Console.WriteLine($"{countA == countB}  countA:{countA}   countB:{countB}");
            Console.ReadLine();

        }

        private static void TestHash()
        {
            var mask = Utils.InitMask(9);
            var sb = new StringBuilder();
            for (int i = 0; i < mask.Length; i++)
            {
                sb.Append(mask[i] + ",");
                if ((i+1) % 16 == 0)
                    sb.AppendLine();
            }
            var json = sb.ToString();
            Console.WriteLine(json);

            for (int i = 0; i <= 16; i++)
            {
                var array = new int[i];
                var rs = new ulong[i];
                for (int n = 0; n < array.Length; n++)
                    array[n] = Random.Shared.Next();
                Utils.Hash(array, 0, array.Length, rs);
            }
        }

         

        

    }
}
