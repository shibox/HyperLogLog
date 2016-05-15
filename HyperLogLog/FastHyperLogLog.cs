using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    public class FastHyperLogLog : IHyperLogLog<string>, IHyperLogLog<int>, IHyperLogLog<uint>,
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
        

        #endregion


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
        /// <param name="hashFunctionId">Type of hash function to use. Default is Murmur3, and FNV-1a is provided for legacy support</param>
        public FastHyperLogLog(int b = 14) : this(CreateEmptyState(b))
        {

        }

        /// <summary>
        ///     Creates a CardinalityEstimator with the given <paramref name="state" />
        /// </summary>
        internal FastHyperLogLog(CardinalityEstimatorState state)
        {
            this.bitsPerIndex = state.BitsPerIndex;
            this.bitsForHll = 64 - this.bitsPerIndex;
            this.m = (int)Math.Pow(2, this.bitsPerIndex);
            this.alphaM = Utils.GetAlphaM(this.m);
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

        #region 公共方法


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

        public void Add(int element)
        {
            HashCode((ulong)element);
        }

        public void Add(uint element)
        {
            HashCode(element);
        }

        public void Add(long element)
        {
            HashCode((ulong)element);
        }

        public void Add(ulong element)
        {
            HashCode(element);
        }

        public void Add(float element)
        {
            HashCode((ulong)element);
        }

        public void Add(double element)
        {
            HashCode((ulong)element);
        }

        public void Add(byte[] element)
        {
            long sum = 0;
            byte overflow;
            for (int i = 0; i < element.Length; i++)
            {
                sum = ((sum << 4) ^ element[i]);
                overflow = (byte)(sum >> 32);
                sum = sum - (((long)overflow) << 32);
                sum = sum ^ overflow;
            }
            if (sum > 2147483647)
                sum = sum - 4294967296;
            Insert((ulong)sum);
        }

        public unsafe void BulkAdd(uint[] values)
        {
            fixed (uint* pd=&values[0])
            {
                uint* pdv = pd;
                for (uint i = 0; i < values.Length; i++)
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

        public void BulkAdd(string[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(int[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(long[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(ulong[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(float[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(double[] values)
        {
            throw new NotImplementedException();
        }

        public void BulkAdd(byte[][] values)
        {
            throw new NotImplementedException();
        }

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
            ushort sub = (ushort)(hash >> bitsForHll);
            byte sigma = 1;
            for (int j = bitsForHll - 1; j >= 0; --j)
            {
                if (((hash >> j) & 1) == 0)
                    sigma++;
                else
                    break;
            }
            if (this.isSparse)
            {
                byte prevRank;
                this.lookupSparse.TryGetValue(sub, out prevRank);
                this.lookupSparse[sub] = Math.Max(prevRank, sigma);
                if (this.lookupSparse.Count > this.sparseMaxElements)
                    SwitchToDenseRepresentation();
            }
            else
                this.lookupDense[sub] = Math.Max(this.lookupDense[sub], sigma);
        }

        /// <summary>
        ///     Merges the given <paramref name="other" /> CardinalityEstimator instance into this one
        /// </summary>
        /// <param name="other">another instance of CardinalityEstimator</param>
        public void Merge(FastHyperLogLog other)
        {
            if (other == null)
            {
                throw new ArgumentNullException("other");
            }

            if (other.m != this.m)
            {
                throw new ArgumentOutOfRangeException("other",
                    "Cannot merge CardinalityEstimator instances with different accuracy/map sizes");
            }

            //this.CountAdditions += other.CountAdditions;
            if (this.isSparse && other.isSparse)
            {
                // Merge two sparse instances
                foreach (KeyValuePair<ushort, byte> kvp in other.lookupSparse)
                {
                    ushort index = kvp.Key;
                    byte otherRank = kvp.Value;
                    byte thisRank;
                    this.lookupSparse.TryGetValue(index, out thisRank);
                    this.lookupSparse[index] = Math.Max(thisRank, otherRank);
                }

                // Switch to dense if necessary
                if (this.lookupSparse.Count > this.sparseMaxElements)
                {
                    SwitchToDenseRepresentation();
                }
            }
            else
            {
                // Make sure this (target) instance is dense, then merge
                SwitchToDenseRepresentation();
                if (other.isSparse)
                {
                    foreach (KeyValuePair<ushort, byte> kvp in other.lookupSparse)
                    {
                        ushort index = kvp.Key;
                        byte rank = kvp.Value;
                        this.lookupDense[index] = Math.Max(this.lookupDense[index], rank);
                    }
                }
                else
                {
                    for (var i = 0; i < this.m; i++)
                    {
                        this.lookupDense[i] = Math.Max(this.lookupDense[i], other.lookupDense[i]);
                    }
                }
            }
        }

        /// <summary>
        ///     Merges the given CardinalityEstimator instances and returns the result
        /// </summary>
        /// <param name="estimators">Instances of CardinalityEstimator</param>
        /// <returns>The merged CardinalityEstimator</returns>
        public static FastHyperLogLog Merge(IList<FastHyperLogLog> estimators)
        {
            if (!estimators.Any())
            {
                throw new ArgumentException(string.Format("Was asked to merge 0 instances of {0}", typeof(FastHyperLogLog)),
                    "estimators");
            }

            var ans = new FastHyperLogLog(estimators[0].bitsPerIndex);
            foreach (FastHyperLogLog estimator in estimators)
            {
                ans.Merge(estimator);
            }

            return ans;
        }

        #endregion


        internal CardinalityEstimatorState GetState()
        {
            return new CardinalityEstimatorState
            {
                BitsPerIndex = this.bitsPerIndex,
                //DirectCount = this.directCount,
                IsSparse = this.isSparse,
                LookupDense = this.lookupDense,
                LookupSparse = this.lookupSparse,
                //HashFunctionId = this.hashFunctionId,
                //CountAdditions = this.CountAdditions,
            };
        }

        /// <summary>
        ///     Creates state for an empty CardinalityEstimator : DirectCount and LookupSparse are empty, LookupDense is null.
        /// </summary>
        /// <param name="b"><see cref="CardinalityEstimator(int, HashFunctionId)" /></param>
        /// <param name="hashFunctionId"><see cref="CardinalityEstimator(int, HashFunctionId)" /></param>
        private static CardinalityEstimatorState CreateEmptyState(int b)
        {
            if (b < 4 || b > 16)
            {
                throw new ArgumentOutOfRangeException("b", b, "Accuracy out of range, legal range is 4 <= BitsPerIndex <= 16");
            }

            return new CardinalityEstimatorState
            {
                BitsPerIndex = b,
                //DirectCount = new HashSet<ulong>(),
                IsSparse = true,
                LookupSparse = new Dictionary<ushort, byte>(),
                LookupDense = null,
                //HashFunctionId = hashFunctionId,
                //CountAdditions = 0,
            };
        }

        /// <summary>
        ///     Adds an element's hash code to the counted set
        /// </summary>
        /// <param name="hashCode">Hash code of the element to add</param>
        //private void AddElementHash(ulong hashCode)
        //{
        //    var sub = (ushort)(hashCode >> this.bitsForHll);
        //    byte sigma = Utils.GetSigma(hashCode, this.bitsForHll);
        //    if (this.isSparse)
        //    {
        //        byte prevRank;
        //        this.lookupSparse.TryGetValue(sub, out prevRank);
        //        this.lookupSparse[sub] = Math.Max(prevRank, sigma);
        //        if (this.lookupSparse.Count > this.sparseMaxElements)
        //        {
        //            SwitchToDenseRepresentation();
        //        }
        //    }
        //    else
        //    {
        //        this.lookupDense[sub] = Math.Max(this.lookupDense[sub], sigma);
        //    }
        //}

        /// <summary>
        ///     Converts this estimator from the sparse to the dense representation
        /// </summary>
        private void SwitchToDenseRepresentation()
        {
            if (!this.isSparse)
                return;

            this.lookupDense = new byte[this.m];
            foreach (KeyValuePair<ushort, byte> kvp in this.lookupSparse)
            {
                int index = kvp.Key;
                this.lookupDense[index] = kvp.Value;
            }
            this.lookupSparse = null;
            this.isSparse = false;
        }

        [OnDeserialized]
        internal void SetHashFunctionAfterDeserializing(StreamingContext context)
        {
            //this.hashFunction = HashFunctionFactory.GetHashFunction(this.hashFunctionId);
        }

    }
}
