using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;

namespace HyperLogLog
{
    /// <summary>
    /// HyperLogLog的一种高性能实现
    /// </summary>
    public class HyperLogLog : IHyperLogLog<string>, IHyperLogLog<int>, IHyperLogLog<uint>,
        IHyperLogLog<long>, IHyperLogLog<ulong>, IHyperLogLog<float>, IHyperLogLog<double>,
        IHyperLogLog<byte[]>
    {
        #region 字段

        private const ulong C1 = 0x87c37b91114253d5UL;
        private const ulong C2 = 0x4cf5ad432745937fUL;
        /// <summary> Number of bits for indexing HLL substreams - the number of estimators is 2^bitsPerIndex </summary>
        private readonly int bitsPerIndex;

        /// <summary> Number of bits to compute the HLL estimate on </summary>
        private readonly int bitsForHll;

        /// <summary> HLL lookup table size </summary>
        private readonly int m;

        /// <summary> Fixed bias correction factor </summary>
        private readonly double alphaM;

        /// <summary> Threshold determining whether to use LinearCounting or HyperLogLog based on an initial estimate </summary>
        private readonly double subAlgorithmSelectionThreshold;

        /// <summary> Lookup table for the dense representation </summary>
        private byte[] lookupDense;

        /// <summary> Lookup dictionary for the sparse representation </summary>
        private IDictionary<ushort, byte> lookupSparse;

        /// <summary> Max number of elements to hold in the sparse representation </summary>
        private readonly int sparseMaxElements;

        /// <summary> Indicates that the sparse representation is currently used </summary>
        private bool isSparse;
        private static byte[] masks9 = new byte[] 
        {
            10,9,8,8,7,7,7,7,6,6,6,6,6,6,6,6,
            5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
            4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
            4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
            3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,3,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,2,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,
            1,1,1,1,1,1,1,1,1,1,1,1,1,1,1,1
        };

        #endregion

        #region 构造函数

        /// <summary>
        ///     C'tor
        /// </summary>
        /// <param name="b">
        ///     Number of bits determining accuracy and memory consumption, in the range [4, 16] (higher = greater accuracy and memory usage).
        ///     For large cardinalities, the standard error is 1.04 * 2^(-b/2), and the memory consumption is bounded by 2^b kilobytes.
        ///     The default value of 14 typically yields 3% error or less across the entire range of cardinalities (usually much less),
        ///     and uses up to ~16kB of memory.  b=4 yields less than ~100% error and uses less than 1kB. b=16 uses up to ~64kB and usually yields 1%
        ///     error or less
        /// </param>
        public HyperLogLog(int b = 14) : this(CreateEmptyState(b))
        {

        }
        
        internal HyperLogLog(EstimatorState state)
        {
            this.bitsPerIndex = state.BitsPerIndex;
            this.bitsForHll = 64 - bitsPerIndex;
            this.m = (int)Math.Pow(2, bitsPerIndex);
            this.alphaM = Utils.GetAlphaM(m);
            this.subAlgorithmSelectionThreshold = Utils.GetAlgorithm(this.bitsPerIndex);
            this.isSparse = state.IsSparse;
            this.lookupSparse = state.LookupSparse != null ? new Dictionary<ushort, byte>(state.LookupSparse) : null;
            this.lookupDense = state.LookupDense;

            // Each element in the sparse representation takes 15 bytes, and there is some constant overhead
            this.sparseMaxElements = Math.Max(0, this.m / 15 - 10);
            // If necessary, switch to the dense representation
            if (this.sparseMaxElements <= 0)
            {
                SwitchToDenseRepresentation();
            }
            InitMask();
        }

        private void InitMask()
        {
            //masks4 = InitMask(4);
            //masks5 = InitMask(5);
            //masks6 = InitMask(6);
            //masks7 = InitMask(7);
            //masks9 = InitMask(9);
        }

        #endregion

        #region 公共方法

        /// <summary>
        /// 添加一个字符串
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public unsafe void Add(string value)
        {
            fixed (char* pd = value)
            {
                char* pdv = pd;
                const ulong fnv1A64Init = 14695981039346656037;
                const ulong fnv64Prime = 0x100000001b3;
                ulong hash = fnv1A64Init;
                for (int i = 0; i < value.Length; i++)
                {
                    hash ^= *pdv;
                    hash *= fnv64Prime;
                    pdv++;
                }
                Insert(hash);
            }
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(int value)
        {
            HashCode((ulong)value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(uint value)
        {
            HashCode(value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(long value)
        {
            HashCode((ulong)value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(ulong value)
        {
            HashCode(value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(float value)
        {
            HashCode((ulong)value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        public void Add(double value)
        {
            HashCode((ulong)value);
        }

        /// <summary>
        /// 添加一个数字
        /// </summary>
        /// <param name="value"></param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Add(byte[] value)
        {
            long sum = 0;
            byte overflow;
            for (int i = 0; i < value.Length; i++)
            {
                sum = ((sum << 4) ^ value[i]);
                overflow = (byte)(sum >> 32);
                sum = sum - (((long)overflow) << 32);
                sum = sum ^ overflow;
            }
            if (sum > 2147483647)
                sum = sum - 4294967296;
            Insert((ulong)sum);
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(uint[] values,int offset,int size)
        {
            fixed (uint* pd=&values[offset])
            {
                uint* pdv = pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void BulkAdd(string[] values, int offset, int size)
        {
            for (int i = offset; i < offset + size; i++)
                Add(values[i]);
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(int[] values, int offset, int size)
        {
            #region 方案1

            fixed (int* pd = &values[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }

            #endregion

            #region 方案2

            //fixed (int* pd = &values[offset])
            //{
            //    byte* lookd = null;
            //    uint* pdv = (uint*)pd;
            //    for (uint i = 0; i < size; i++)
            //    {
            //        ulong hash = *pdv++;
            //        hash = (hash * C1);
            //        hash ^= ((hash << 31) | (hash >> 33)) * C2;
            //        hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
            //        hash = (hash ^ (hash >> 33));

            //        ushort sub = (ushort)(hash >> bitsForHll);
            //        byte sigma = 1;
            //        if (((hash << 14) >> 60) != 0)
            //            sigma = masks[(hash << 14) >> 60];
            //        else
            //        {
            //            for (int j = 49; j >= 0; --j)
            //            {
            //                if (((hash >> j) & 1) == 0)
            //                    sigma++;
            //                else
            //                    break;
            //            }
            //        }
            //        if (isSparse)
            //        {
            //            byte prevRank;
            //            lookupSparse.TryGetValue(sub, out prevRank);
            //            lookupSparse[sub] = Math.Max(prevRank, sigma);
            //            if (lookupSparse.Count > this.sparseMaxElements)
            //            {
            //                SwitchToDenseRepresentation();
            //                fixed (byte* ld = &lookupDense[0])
            //                {
            //                    lookd = ld;
            //                }
            //            }
            //        }
            //        else if (lookd[sub] < sigma)
            //            lookd[sub] = sigma;
            //    }
            //}

            #endregion

            //counts.ToString();
            //List<KeyValuePair<byte, int>> rs = counts.ToList<KeyValuePair<byte, int>>();
            //rs.Sort(delegate (KeyValuePair<byte, int>  x, KeyValuePair<byte, int> y) { return x.Value.CompareTo(y.Value); });
            //StringBuilder sb = new StringBuilder();
            //int c = rs.Sum(item => item.Value);
            //for (int i = 0; i < rs.Count; i++)
            //    sb.AppendLine(rs[i].Key + " " + rs[i].Value + " " + ((double)rs[i].Value / (double)c).ToString("f4"));
            //Console.WriteLine(sb.ToString());
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(long[] values, int offset, int size)
        {
            fixed (long* pd = &values[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(ulong[] values, int offset, int size)
        {
            fixed (ulong* pd = &values[offset])
            {
                ulong* pdv = pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(float[] values, int offset, int size)
        {
            fixed (float* pd = &values[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void BulkAdd(double[] values, int offset, int size)
        {
            fixed (double* pd = &values[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 批量添加
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public void BulkAdd(byte[][] values, int offset, int size)
        {
            for (int i = offset; i < offset + size; i++)
                Add(values[i]);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsInt(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 4 == 0)
                AddAsInt(buffer, 0, count >> 2);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsUInt(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 4 == 0)
                AddAsUInt(buffer, 0, count >> 2);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsLong(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 8 == 0)
                AddAsLong(buffer, 0, count >> 3);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsULong(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 8 == 0)
               AddAsULong(buffer, 0, count >> 3);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsFloat(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 4 == 0)
                AddAsFloat(buffer, 0, count >> 2);
        }

        /// <summary>
        /// 以流模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        public void AddAsDouble(Stream value)
        {
            byte[] buffer = new byte[1024];
            int count = 0;
            while ((count = value.Read(buffer, 0, buffer.Length)) > 0 && count % 8 == 0)
                AddAsDouble(buffer, 0, count >> 3);
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsInt(byte[] value, int offset, int size)
        {
            ulong hash = 0;
            fixed (byte* pd = &value[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i++)
                {
                    hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsUInt(byte[] value, int offset, int size)
        {
            fixed (byte* pd = &value[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsLong(byte[] value, int offset, int size)
        {
            fixed (byte* pd = &value[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsULong(byte[] value, int offset, int size)
        {
            fixed (byte* pd = &value[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsFloat(byte[] value, int offset, int size)
        {
            fixed (byte* pd = &value[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 以二进制模式的数据添加
        /// </summary>
        /// <param name="value"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        public unsafe void AddAsDouble(byte[] value, int offset, int size)
        {
            fixed (byte* pd = &value[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i++)
                {
                    ulong hash = *pdv++;
                    hash = (hash * C1);
                    hash ^= ((hash << 31) | (hash >> 33)) * C2;
                    hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                    hash = (hash ^ (hash >> 33));
                    Insert(hash);
                }
            }
        }

        /// <summary>
        /// 获得估值结果数量
        /// </summary>
        /// <returns></returns>
        public ulong Count()
        {
            double zInverse = 0;
            double v = 0;

            if (this.isSparse)
            {
                // calc c and Z's inverse
                foreach (KeyValuePair<ushort, byte> kvp in this.lookupSparse)
                {
                    byte sigma = kvp.Value;
                    zInverse += Math.Pow(2, -sigma);
                }
                v = this.m - this.lookupSparse.Count;
                zInverse += (this.m - this.lookupSparse.Count);
            }
            else
            {
                // calc c and Z's inverse
                for (var i = 0; i < this.m; i++)
                {
                    byte sigma = this.lookupDense[i];
                    zInverse += Math.Pow(2, -sigma);
                    if (sigma == 0)
                    {
                        v++;
                    }
                }
            }

            double e = this.alphaM * this.m * this.m / zInverse;
            if (e <= 5.0 * this.m)
            {
                e = BiasCorrection.CorrectBias(e, this.bitsPerIndex);
            }

            double h;
            if (v > 0)
            {
                // LinearCounting estimate
                h = this.m * Math.Log(this.m / v);
            }
            else
            {
                h = e;
            }

            if (h <= this.subAlgorithmSelectionThreshold)
            {
                return (ulong)Math.Round(h);
            }
            return (ulong)Math.Round(e);
        }

        /// <summary>
        /// 与另一个估值计数器合并
        /// </summary>
        /// <param name="other"></param>
        public void Merge(HyperLogLog other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            if (other.m != m)
            {
                throw new ArgumentOutOfRangeException("other",
                    "Cannot merge CardinalityEstimator instances with different accuracy/map sizes");
            }

            if (this.isSparse && other.isSparse)
            {
                foreach (KeyValuePair<ushort, byte> kvp in other.lookupSparse)
                {
                    ushort index = kvp.Key;
                    byte otherRank = kvp.Value;
                    byte thisRank;
                    lookupSparse.TryGetValue(index, out thisRank);
                    lookupSparse[index] = Math.Max(thisRank, otherRank);
                }
                if (lookupSparse.Count > sparseMaxElements)
                {
                    SwitchToDenseRepresentation();
                }
            }
            else
            {
                SwitchToDenseRepresentation();
                if (other.isSparse)
                {
                    foreach (KeyValuePair<ushort, byte> kvp in other.lookupSparse)
                    {
                        ushort index = kvp.Key;
                        byte rank = kvp.Value;
                        this.lookupDense[index] = Math.Max(lookupDense[index], rank);
                    }
                }
                else
                {
                    for (var i = 0; i < m; i++)
                    {
                        lookupDense[i] = Math.Max(lookupDense[i], other.lookupDense[i]);
                    }
                }
            }
        }

        /// <summary>
        /// 合并多个估值计数器
        /// </summary>
        /// <param name="estimators"></param>
        /// <returns></returns>
        public static HyperLogLog Merge(IList<HyperLogLog> estimators)
        {
            if (!estimators.Any())
            {
                throw new ArgumentException(string.Format("Was asked to merge 0 instances of {0}", typeof(HyperLogLog)),
                    "estimators");
            }

            var ans = new HyperLogLog(estimators[0].bitsPerIndex);
            foreach (HyperLogLog estimator in estimators)
            {
                ans.Merge(estimator);
            }

            return ans;
        }

        #endregion

        #region 私有

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void HashCode(ulong hash)
        {
            hash = (hash * C1);
            hash ^= ((hash << 31) | (hash >> 33)) * C2;
            hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
            hash = (hash ^ (hash >> 33));
            Insert(hash);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Insert(ulong hash)
        {
            //新方案使用移位代替条件判断，大幅度优化了性能，一个典型的测试，优化前：142ms，优化后，整体耗时：66ms，这其中哈希计算大约耗时：17ms
            ushort sub = (ushort)(hash >> bitsForHll);
            byte sigma = 1;

            #region 优化1

            //int pos = (int)((hash << 14) >> 60);
            //if (pos != 0)
            //    sigma = masks[(hash << 14) >> 60];
            //else
            //{
            //    for (int j = 49; j >= 0; --j)
            //    {
            //        if (((hash >> j) & 1) == 0)
            //            sigma++;
            //        else
            //            break;
            //    }
            //}

            #endregion

            #region 优化2

            //if (((hash << 14) >> 60) != 0)
            //    sigma = masks[(hash << 14) >> 60];
            //else
            //{
            //    for (int j = 49; j >= 0; --j)
            //    {
            //        if (((hash >> j) & 1) == 0)
            //            sigma++;
            //        else
            //            break;
            //    }
            //}

            #endregion

            #region old

            //{ 5, 4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
            for (int j = bitsForHll - 1; j >= 0; --j)
            {
                if (((hash >> j) & 1) == 0)
                    sigma++;
                else
                    break;
            }
            //if (counts.ContainsKey(sigma) == false)
            //    counts.Add(sigma, 1);
            //else
            //    counts[sigma]++;

            #endregion

            if (isSparse)
            {
                byte prevRank;
                lookupSparse.TryGetValue(sub, out prevRank);
                lookupSparse[sub] = Math.Max(prevRank, sigma);
                if (lookupSparse.Count > this.sparseMaxElements)
                    SwitchToDenseRepresentation();
            }
            else if (lookupDense[sub] < sigma)
                lookupDense[sub] = sigma;
        }

        internal EstimatorState GetState()
        {
            return new EstimatorState
            {
                BitsPerIndex = bitsPerIndex,
                IsSparse = isSparse,
                LookupDense = lookupDense,
                LookupSparse = lookupSparse,
            };
        }

        private static EstimatorState CreateEmptyState(int b)
        {
            if (b < 4 || b > 16)
            {
                throw new ArgumentOutOfRangeException("b", b, "Accuracy out of range, legal range is 4 <= BitsPerIndex <= 16");
            }
            return new EstimatorState
            {
                BitsPerIndex = b,
                IsSparse = true,
                LookupSparse = new Dictionary<ushort, byte>(),
                LookupDense = null,
            };
        }

        private void SwitchToDenseRepresentation()
        {
            if (!isSparse)
                return;

            lookupDense = new byte[this.m];
            foreach (KeyValuePair<ushort, byte> kvp in lookupSparse)
            {
                int index = kvp.Key;
                lookupDense[index] = kvp.Value;
            }
            lookupSparse = null;
            isSparse = false;
        }

        #endregion

        #region 静态方法

        public static unsafe ulong Count14(ulong[] values, int offset, int size)
        {
            int bitsForHll = 50;
            int m = 16384;
            int* mask = stackalloc int[128 * 4];
            for (int i = 0; i < masks9.Length; i+=0)
            {
                mask[i + 0] = masks9[i + 0];
                mask[i + 1] = masks9[i + 1];
                mask[i + 2] = masks9[i + 2];
                mask[i + 3] = masks9[i + 3];
            }
                
            byte* lookd = stackalloc byte[m];
            fixed (ulong* pd = &values[offset])
            {
                ulong* pdv = (ulong*)pd;
                for (uint i = 0; i < size; i += 4)
                {
                    ulong hash1 = *pdv++;
                    ulong hash2 = *pdv++;
                    ulong hash3 = *pdv++;
                    ulong hash4 = *pdv++;

                    //int sigma1 = Utils.GetSigma(hash1, masks9);
                    //int sigma2 = Utils.GetSigma(hash2, masks9);
                    //int sigma3 = Utils.GetSigma(hash3, masks9);
                    //int sigma4 = Utils.GetSigma(hash4, masks9);

                    #region sigma
                    int sigma1 = 0;
                    int sigma2 = 0;
                    int sigma3 = 0;
                    int sigma4 = 0;
                    ulong pos = ((hash1 << 14) >> 55);
                    if (pos != 0)
                        sigma1 += mask[pos];
                    else
                    {
                        sigma1 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash1 >> j) & 1) == 0)
                                sigma1++;
                            else
                                break;
                        }
                    }
                    pos = ((hash2 << 14) >> 55);
                    if (pos != 0)
                        sigma2 += mask[pos];
                    else
                    {
                        sigma2 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash2 >> j) & 1) == 0)
                                sigma2++;
                            else
                                break;
                        }
                    }

                    pos = ((hash3 << 14) >> 55);
                    if (pos != 0)
                        sigma3 += mask[pos];
                    else
                    {
                        sigma3 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash3 >> j) & 1) == 0)
                                sigma3++;
                            else
                                break;
                        }
                    }

                    pos = ((hash4 << 14) >> 55);
                    if (pos != 0)
                        sigma4 += mask[pos];
                    else
                    {
                        sigma4 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash4 >> j) & 1) == 0)
                                sigma4++;
                            else
                                break;
                        }
                    }
                    #endregion

                    hash1 = (hash1 >> bitsForHll);
                    hash2 = (hash2 >> bitsForHll);
                    hash3 = (hash3 >> bitsForHll);
                    hash4 = (hash4 >> bitsForHll);

                    if (lookd[hash1] < sigma1)
                        lookd[hash1] = (byte)sigma1;
                    if (lookd[hash2] < sigma2)
                        lookd[hash2] = (byte)sigma2;
                    if (lookd[hash3] < sigma3)
                        lookd[hash3] = (byte)sigma3;
                    if (lookd[hash4] < sigma4)
                        lookd[hash4] = (byte)sigma4;
                }
            }
            return Count(lookd);
        }

        public static unsafe ulong Count14(int[] values, int offset, int size)
        {
            ulong n = 0;
            int bitsForHll = 50;
            int m = 16384;
            int* mask = stackalloc int[128 * 4];
            for (int i = 0; i < masks9.Length; i++)
                mask[i] = masks9[i];
            byte* lookd = stackalloc byte[m];
            fixed (int* pd = &values[offset])
            {
                uint* pdv = (uint*)pd;
                for (uint i = 0; i < size; i += 4)
                {
                    ulong hash1 = *pdv++;
                    ulong hash2 = *pdv++;
                    ulong hash3 = *pdv++;
                    ulong hash4 = *pdv++;

                    hash1 = (hash1 * C1);
                    hash2 = (hash2 * C1);
                    hash3 = (hash3 * C1);
                    hash4 = (hash4 * C1);

                    hash1 ^= ((hash1 << 31) | (hash1 >> 33)) * C2;
                    hash2 ^= ((hash2 << 31) | (hash2 >> 33)) * C2;
                    hash3 ^= ((hash3 << 31) | (hash3 >> 33)) * C2;
                    hash4 ^= ((hash4 << 31) | (hash4 >> 33)) * C2;

                    hash1 = (hash1 ^ (hash1 >> 33)) * 0xff51afd7ed558ccd;
                    hash2 = (hash2 ^ (hash2 >> 33)) * 0xff51afd7ed558ccd;
                    hash3 = (hash3 ^ (hash3 >> 33)) * 0xff51afd7ed558ccd;
                    hash4 = (hash4 ^ (hash4 >> 33)) * 0xff51afd7ed558ccd;

                    hash1 = (hash1 ^ (hash1 >> 33));
                    hash2 = (hash2 ^ (hash2 >> 33));
                    hash3 = (hash3 ^ (hash3 >> 33));
                    hash4 = (hash4 ^ (hash4 >> 33));

                    #region sigma
                    int sigma1 = 0;
                    int sigma2 = 0;
                    int sigma3 = 0;
                    int sigma4 = 0;
                    ulong pos = ((hash1 << 14) >> 55);
                    if (pos != 0)
                        sigma1 += mask[pos];
                    else
                    {
                        sigma1 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash1 >> j) & 1) == 0)
                                sigma1++;
                            else
                                break;
                        }
                    }
                    pos = ((hash2 << 14) >> 55);
                    if (pos != 0)
                        sigma2 += mask[pos];
                    else
                    {
                        sigma2 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash2 >> j) & 1) == 0)
                                sigma2++;
                            else
                                break;
                        }
                    }

                    pos = ((hash3 << 14) >> 55);
                    if (pos != 0)
                        sigma3 += mask[pos];
                    else
                    {
                        sigma3 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash3 >> j) & 1) == 0)
                                sigma3++;
                            else
                                break;
                        }
                    }

                    pos = ((hash4 << 14) >> 55);
                    if (pos != 0)
                        sigma4 += mask[pos];
                    else
                    {
                        sigma4 = 1;
                        for (int j = 49; j >= 0; --j)
                        {
                            if (((hash4 >> j) & 1) == 0)
                                sigma4++;
                            else
                                break;
                        }
                    }
                    #endregion

                    hash1 = (hash1 >> bitsForHll);
                    hash2 = (hash2 >> bitsForHll);
                    hash3 = (hash3 >> bitsForHll);
                    hash4 = (hash4 >> bitsForHll);

                    if (lookd[hash1] < sigma1)
                        lookd[hash1] = (byte)sigma1;
                    if (lookd[hash2] < sigma2)
                        lookd[hash2] = (byte)sigma2;
                    if (lookd[hash3] < sigma3)
                        lookd[hash3] = (byte)sigma3;
                    if (lookd[hash4] < sigma4)
                        lookd[hash4] = (byte)sigma4;
                }
            }
            return n;

        }

        private unsafe static ulong Count(byte* lookd, int m = 16384, int bitsPerIndex = 14)
        {
            double alpha = Utils.GetAlphaM(m);
            double alg = Utils.GetAlgorithm(bitsPerIndex);

            double zInverse = 0;
            double v = 0;
            for (var i = 0; i < m; i++)
            {
                byte sigma = lookd[i];
                zInverse += Math.Pow(2, -sigma);
                if (sigma == 0)
                    v++;
            }
            double e = alpha * m * m / zInverse;
            if (e <= 5.0 * m)
                e = BiasCorrection.CorrectBias(e, bitsPerIndex);

            double h;
            if (v > 0)
                h = m * Math.Log(m / v);
            else
                h = e;

            if (h <= alg)
                return (ulong)Math.Round(h);
            return (ulong)Math.Round(e);
        }

        #endregion





    }
}
