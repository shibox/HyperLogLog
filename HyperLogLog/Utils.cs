using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.Intrinsics.X86;

namespace HyperLogLog
{
    public static class Utils
    {
        private const ulong C1 = 0x87c37b91114253d5UL;
        private const ulong C2 = 0x4cf5ad432745937fUL;

        /// <summary>
        /// 批量对数据生成哈希
        /// </summary>
        /// <param name="values"></param>
        /// <param name="offset"></param>
        /// <param name="size"></param>
        /// <param name="rs"></param>
        public static unsafe void Hash(int[] values, int offset, int size, ulong[] rs)
        {
            Hash(values, offset, size, rs, 0);
        }

        public static unsafe void Hash(int[] values, int offset, int size, ulong[] rs,int rsOffset)
        {
            if (offset + size > values.Length || rsOffset + size > rs.Length || offset < 0 | size < 0 | rsOffset < 0)
                throw new ArgumentOutOfRangeException();
            if (values.Length == offset)
                return;
            fixed (int* pd = &values[offset])
            {
                fixed (ulong* rst = &rs[rsOffset])
                {
                    ulong* dst = rst;
                    uint* pdv = (uint*)pd;
                    uint i = 0;
                    for (; i < size / 4 * 4; i += 4)
                    {
                        ulong hash1 = *pdv++;
                        ulong hash2 = *pdv++;
                        ulong hash3 = *pdv++;
                        ulong hash4 = *pdv++;

                        hash1 *= C1;
                        hash2 *= C1;
                        hash3 *= C1;
                        hash4 *= C1;

                        hash1 ^= ((hash1 << 31) | (hash1 >> 33)) * C2;
                        hash2 ^= ((hash2 << 31) | (hash2 >> 33)) * C2;
                        hash3 ^= ((hash3 << 31) | (hash3 >> 33)) * C2;
                        hash4 ^= ((hash4 << 31) | (hash4 >> 33)) * C2;

                        hash1 = (hash1 ^ (hash1 >> 33)) * 0xff51afd7ed558ccd;
                        hash2 = (hash2 ^ (hash2 >> 33)) * 0xff51afd7ed558ccd;
                        hash3 = (hash3 ^ (hash3 >> 33)) * 0xff51afd7ed558ccd;
                        hash4 = (hash4 ^ (hash4 >> 33)) * 0xff51afd7ed558ccd;

                        hash1 ^= (hash1 >> 33);
                        hash2 ^= (hash2 >> 33);
                        hash3 ^= (hash3 >> 33);
                        hash4 ^= (hash4 >> 33);

                        *(dst + 0) = hash1;
                        *(dst + 1) = hash2;
                        *(dst + 2) = hash3;
                        *(dst + 3) = hash4;
                        dst += 4;
                    }
                    for (; i < size; i++)
                    {
                        ulong hash = *pdv++;
                        hash *= C1;
                        hash ^= ((hash << 31) | (hash >> 33)) * C2;
                        hash = (hash ^ (hash >> 33)) * 0xff51afd7ed558ccd;
                        hash ^= (hash >> 33);
                        *dst++ = hash;
                    }
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSigmaLookup(ulong hash, byte[] mask)
        {
            int sigma = 0;
            ulong pos = ((hash << 14) >> 55);
            if (pos != 0)
                sigma += mask[pos];
            else
            {
                sigma = 1;
                for (int j = 49; j >= 0; --j)
                {
                    if (((hash >> j) & 1) == 0)
                        sigma++;
                    else
                        break;
                }
            }
            return sigma;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSigmaCommon(ulong hash)
        {
            int sigma = 1;
            for (int j = 49; j >= 0; --j)
            {
                if (((hash >> j) & 1) == 0)
                    sigma++;
                else
                    break;
            }
            return sigma;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int GetSigmaLeading(ulong hash)
        {
            return 1 + (int)Lzcnt.X64.LeadingZeroCount(hash << 14);
        }

        /// <summary>
        /// 分支判断的数量统计，当n取9时，99%的数据可以通过直接查表获得，极大的提升了性能
        /// 51 1 0.0000
        /// 25 1 0.0000
        /// 22 1 0.0000
        /// 24 1 0.0000
        /// 23 1 0.0000
        /// 21 5 0.0000
        /// 20 8 0.0000
        /// 19 26 0.0000
        /// 18 27 0.0000
        /// 17 94 0.0000
        /// 16 163 0.0000
        /// 15 290 0.0000
        /// 14 616 0.0001
        /// 13 1204 0.0001
        /// 12 2400 0.0002
        /// 11 4798 0.0005
        /// 10 9863 0.0010
        /// 9 19543 0.0020
        /// 8 38886 0.0039
        /// 7 78156 0.0078
        /// 6 156677 0.0157
        /// 5 313447 0.0313
        /// 4 625748 0.0626
        /// 3 1250154 0.1250
        /// 2 2498390 0.2498
        /// 1 4999500 0.5000
        /// </summary>
        /// <param name="nbit"></param>
        /// <returns></returns>
        public static byte[] InitMask(int nbit)
        {
            var mask = new byte[1 << nbit];
            for (int v = 0; v < mask.Length; v++)
            {
                var s = Convert.ToString(v, 2).PadLeft(nbit, '0');
                var sigma = 1;
                for (int i = 0; i < s.Length; i++)
                {
                    if (s[i] == '0')
                        sigma++;
                    else
                        break;
                }
                mask[v] = (byte)sigma;
            }
            return mask;
        }

        /// <summary>
        ///     Gets the appropriate value of alpha_M for the given <paramref name="m" />
        /// </summary>
        /// <param name="m">size of the lookup table</param>
        /// <returns>alpha_M for bias correction</returns>
        internal static double GetAlphaM(int m)
        {
            switch (m)
            {
                case 16:
                    return 0.673;
                case 32:
                    return 0.697;
                case 64:
                    return 0.709;
                default:
                    return 0.7213 / (1 + 1.079 / m);
            }
        }

        /// <summary>
        ///     Returns the base-2 logarithm of <paramref name="x" />.
        ///     This implementation is faster than <see cref="Math.Log(double,double)" /> as it avoids input checks
        /// </summary>
        /// <param name="x"></param>
        /// <returns>The base-2 logarithm of <paramref name="x" /></returns>
        internal static double Log2(double x)
        {
            const double ln2 = 0.693147180559945309417232121458;
            return Math.Log(x) / ln2;
        }

        /// <summary>
        ///     Returns the threshold determining whether to use LinearCounting or HyperLogLog for an estimate. Values are from the supplementary
        ///     material of Huele et al.,
        ///     <see cref="http://docs.google.com/document/d/1gyjfMHy43U9OWBXxfaeG-3MjGzejW1dlpyMwEYAAWEI/view?fullscreen#heading=h.nd379k1fxnux" />
        /// </summary>
        /// <param name="bits">Number of bits</param>
        /// <returns></returns>
        internal static double GetAlgorithm(int bits)
        {
            switch (bits)
            {
                case 4:
                    return 10;
                case 5:
                    return 20;
                case 6:
                    return 40;
                case 7:
                    return 80;
                case 8:
                    return 220;
                case 9:
                    return 400;
                case 10:
                    return 900;
                case 11:
                    return 1800;
                case 12:
                    return 3100;
                case 13:
                    return 6500;
                case 14:
                    return 11500;
                case 15:
                    return 20000;
                case 16:
                    return 50000;
                case 17:
                    return 120000;
                case 18:
                    return 350000;
            }
            throw new ArgumentOutOfRangeException("bits", "Unexpected number of bits (should never happen)");
        }

        internal unsafe static ulong Count(byte[] look, int m = 16384, int bitsPerIndex = 14)
        {
            fixed (byte* p = look)
                return Count(p, m, bitsPerIndex);
        }

        internal unsafe static ulong Count(byte* look, int m = 16384, int bitsPerIndex = 14)
        {
            double alpha = GetAlphaM(m);
            double alg = GetAlgorithm(bitsPerIndex);

            double zInverse = 0;
            double v = 0;
            for (var i = 0; i < m; i++)
            {
                byte sigma = look[i];
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

    }
}
