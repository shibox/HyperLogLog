using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    internal static class Utils
    {

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
        ///     Returns the number of leading zeroes in the <paramref name="bitsToCount" /> highest bits of <paramref name="hash" />, plus one
        /// </summary>
        /// <param name="hash">Hash value to calculate the statistic on</param>
        /// <param name="bitsToCount">Lowest bit to count from <paramref name="hash" /></param>
        /// <returns>The number of leading zeroes in the binary representation of <paramref name="hash" />, plus one</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static byte GetSigma(ulong hash, int bitsToCount)
        {
            byte sigma = 1;
            for (int i = bitsToCount - 1; i >= 0; --i)
            {
                if (((hash >> i) & 1) == 0)
                {
                    sigma++;
                }
                else
                {
                    break;
                }
            }
            return sigma;
        }

        /// <summary>
        ///     Returns the threshold determining whether to use LinearCounting or HyperLogLog for an estimate. Values are from the supplementary
        ///     material of Huele et al.,
        ///     <see cref="http://docs.google.com/document/d/1gyjfMHy43U9OWBXxfaeG-3MjGzejW1dlpyMwEYAAWEI/view?fullscreen#heading=h.nd379k1fxnux" />
        /// </summary>
        /// <param name="bits">Number of bits</param>
        /// <returns></returns>
        internal static double GetSubAlgorithmSelectionThreshold(int bits)
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

//#if NETFX45
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//#endif
//        internal static uint RotateLeft(this uint x, byte r)
//        {
//            return (x << r) | (x >> (32 - r));
//        }

//#if NETFX45
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//#endif
//        internal static ulong RotateLeft(this ulong x, byte r)
//        {
//            return (x << r) | (x >> (64 - r));
//        }

//#if NETFX45
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//#endif
//        internal static uint FMix(this uint h)
//        {
//            // pipelining friendly algorithm
//            h = (h ^ (h >> 16)) * 0x85ebca6b;
//            h = (h ^ (h >> 13)) * 0xc2b2ae35;
//            return h ^ (h >> 16);
//        }

//#if NETFX45
//        [MethodImpl(MethodImplOptions.AggressiveInlining)]
//#endif
//        internal static ulong FMix(this ulong h)
//        {
//            // pipelining friendly algorithm
//            h = (h ^ (h >> 33)) * 0xff51afd7ed558ccd;
//            h = (h ^ (h >> 33)) * 0xc4ceb9fe1a85ec53;

//            return (h ^ (h >> 33));
//        }


    }
}
