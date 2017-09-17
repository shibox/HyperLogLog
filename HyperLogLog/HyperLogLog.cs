using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;

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
        private static readonly byte[] masks = new byte[] { 5, 4, 3, 3, 2, 2, 2, 2, 1, 1, 1, 1, 1, 1, 1, 1 };
        int c = 0;

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
            this.subAlgorithmSelectionThreshold = Utils.GetSubAlgorithmSelectionThreshold(this.bitsPerIndex);
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
                    c++;
                    //Insert(hash);
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
            //优化后，
            ushort sub = (ushort)(hash >> bitsForHll);
            byte sigma = 1;
            int pos = (int)((hash << 14) >> 60);
            if (pos != 0)
                sigma = masks[(hash << 14) >> 60];
            else
            {
                for (int j = 49; j >= 0; --j)
                {
                    if (((hash >> j) & 1) == 0)
                        sigma++;
                    else
                        break;
                }
            }
            #region old
            //for (int j = bitsForHll - 1; j >= 0; --j)
            //{
            //    if (((hash >> j) & 1) == 0)
            //        sigma++;
            //    else
            //        break;
            //}
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


    }
}
