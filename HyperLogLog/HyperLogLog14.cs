using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics.X86;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    /// <summary>
    /// 占用2^14空间（16KB）的基数估值计算器
    /// 准确性：平均大概误差不到1%
    /// 1千元素：avg error rate:0.45 %
    /// 1万元素：avg error rate:0.49 %
    /// 2万元素：avg error rate:0.55 %
    /// 5万元素：avg error rate:0.53 %
    /// 10万元素：avg error rate:0.60 %
    /// </summary>
    public unsafe class HyperLogLog14:IDisposable
    {
        const int bitsForHll = 50;
        const int m = 16384;
        const int page = 4096;
        byte* look;
        void * lookPtr;

        public HyperLogLog14()
        {
            //lookPtr = NativeMemory.AlignedAlloc(m, page);
            //lookPtr = NativeMemory.Alloc(m);
            lookPtr = NativeMemory.AllocZeroed(m);
            look = (byte*)lookPtr;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(ulong hash)
        {
            var sigma = 1 + Lzcnt.X64.LeadingZeroCount(hash << 14);
            hash >>= bitsForHll;
            if (look[hash] < sigma)
                look[hash] = (byte)sigma;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Insert(Span<ulong> hashs)
        {
            fixed (ulong* hashsPtr = hashs)
            {
                var offset = 0;
                while (offset < hashs.Length)
                {
                    var hash = hashsPtr[offset++];
                    var sigma = 1 + Lzcnt.X64.LeadingZeroCount(hash << 14);
                    hash >>= bitsForHll;
                    if (look[hash] < sigma)
                        look[hash] = (byte)sigma;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public ulong Count()
        {
            return Utils.Count(look);
        }

        public void Dispose()
        {
            NativeMemory.Free(lookPtr);
        }
    }
}
