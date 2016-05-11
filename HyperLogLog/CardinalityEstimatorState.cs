using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    internal class CardinalityEstimatorState
    {
        public int BitsPerIndex;
        public HashSet<ulong> DirectCount;
        public bool IsSparse;
        public byte[] LookupDense;
        public IDictionary<ushort, byte> LookupSparse;
        public ulong CountAdditions;
    }
}
