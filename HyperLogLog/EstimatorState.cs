using System.Collections.Generic;

namespace HyperLogLog
{
    internal class EstimatorState
    {
        public int BitsPerIndex;
        public bool IsSparse;
        public byte[] LookupDense;
        public IDictionary<ushort, byte> LookupSparse;
    }
}
