using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
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
            TestBenchmark();
            //TestValidity();
            //TestHash();
        }

        private static void TestCountUInt32()
        {
            HyperLogLog estimator = new HyperLogLog();
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
            HyperLogLog estimator = new HyperLogLog();
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
            HyperLogLog estimator = new HyperLogLog();
            ulong[] array = new ulong[100000000];
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
            HyperLogLog estimator = new HyperLogLog();
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
            HyperLogLog estimator = new HyperLogLog();
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
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            for (int n = 0; n < 100; n++)
            {
                for (int i = 0; i < array.Length; i++)
                    array[i] = rd.Next();
                Utils.Hash(array, 0, array.Length, rs);
                CheckSigma(rs, 0, rs.Length);
            }
        }

        private static void TestBenchmark()
        {
            StreamWriter writer = new StreamWriter("result.log");
            int count = 0;
            int[] array = new int[10000000];
            ulong[] rs = new ulong[array.Length];
            Random rd = new Random(Guid.NewGuid().GetHashCode());
            for (int n = 0; n < 10000; n++)
            {
                Dictionary<int, bool> map = new Dictionary<int, bool>(array.Length);
                for (int i = 0; i < array.Length; i++)
                {
                    array[i] = rd.Next();
                    if (map.ContainsKey(array[i]) == false)
                        map.Add(array[i], true);
                }
                Stopwatch w = Stopwatch.StartNew();
                Utils.Hash(array, 0, array.Length, rs);
                string s = $"hash cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                writer.WriteLine(s);
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine(s);
                w = Stopwatch.StartNew();
                count = (int)HyperLogLog.Count14(rs, 0, rs.Length);
                w.Stop();

                s = $"real count:{map.Count.ToString().PadLeft(8, ' ')} estimator count:{count.ToString().PadLeft(8, ' ')}  cost:{w.ElapsedMilliseconds.ToString().PadLeft(3, ' ')}";
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine(s);

                s = $"error rate:{((1.0 - (map.Count / (float)count)) * 100).ToString("f4").PadLeft(7, ' ')}%";
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine(s);

                if (n % 100 == 0)
                    writer.Flush();
            }
            Console.ReadLine();

        }

        private static void TestValidity()
        {
            int countA = 0,countB=0;
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

            Utils.Hash(array, 0, array.Length, rs);
            Stopwatch w = Stopwatch.StartNew();
            countA = (int)HyperLogLog.Count14(rs, 0, rs.Length);

            HyperLogLog estimator = new HyperLogLog();
            estimator.BulkAdd(array, 0, array.Length);
            countB = (int)estimator.Count();
            
            Console.WriteLine("real count:" + map.Count + " estimator count:" + countA + "    cost:" + w.ElapsedMilliseconds);
            Console.WriteLine("error rate:" + ((1.0 - (map.Count / (float)countA)) * 100).ToString("f4"));
            Console.WriteLine($"{countA == countB}  countA:{countA}   countB:{countB}");
            Console.ReadLine();

        }

        private static void TestHash()
        {
            byte[] mask = Utils.InitMask(9);
            StringBuilder sb = new StringBuilder();
            for (int i = 0; i < mask.Length; i++)
            {
                sb.Append(mask[i] + ",");
                if ((i+1) % 16 == 0)
                    sb.AppendLine();
            }
            string json = sb.ToString();
            Console.WriteLine(json);

            for (int i = 0; i <= 16; i++)
            {
                int[] array = new int[i];
                ulong[] rs = new ulong[i];
                Random rd = new Random(Guid.NewGuid().GetHashCode());
                for (int n = 0; n < array.Length; n++)
                    array[n] = rd.Next();
                Utils.Hash(array, 0, array.Length, rs);
                Console.WriteLine(JsonConvert.SerializeObject(rs));
                
            }
        }

        public static void CheckSigma(ulong[] values, int offset, int size)
        {
            int error = 0;
            byte[] mask = Utils.InitMask(9);
            for (uint i = 0; i < size; i++)
            {
                ulong hash = values[i+offset];
                int sigmaA = Utils.GetSigma(hash, mask);
                int sigmaB = Utils.GetSigma(hash);
                if (sigmaA != sigmaB)
                    error++;
            }
            Console.WriteLine($"error:{error}");
        }

    }
}
