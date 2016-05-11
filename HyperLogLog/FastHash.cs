﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HyperLogLog
{
    internal class FastHash : IHashFunction
    {
        #region 字段

        const ulong C1 = 0x87c37b91114253d5UL;
        const ulong C2 = 0x4cf5ad432745937fUL;

        byte[] result = new byte[16];

        #endregion

        #region 属性

        private ulong H1 { get; set; }
        private ulong H2 { get; set; }
        private int Length { get; set; }

        #endregion

        #region 构造函数

        internal FastHash(uint seed = 0)
        {

        }

        #endregion

        #region 方法

        private void Reset()
        {
            H1 = H2 = 0;
            Length = 0;
        }

        protected void HashCore(byte[] array, int ibStart, int cbSize)
        {
            // store the length of the hash (for use later)
            Length += cbSize;
            Body(array, ibStart, cbSize);
        }

#if NETFX45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        private void Body(byte[] data, int start, int length)
        {
            int remainder = length & 15;
            int blocks = length / 16;

            unsafe
            {
                fixed (byte* d = &data[start])
                {
                    ulong* current = (ulong*)d;

                    while (blocks-- > 0)
                    {
                        // a variant of original algorithm optimized for processor instruction pipelining
                        H1 ^= (*current++ * C1).RotateLeft(31) * C2;
                        H1 = (H1.RotateLeft(27) + H2) * 5 + 0x52dce729;

                        H2 ^= (*current++ * C2).RotateLeft(33) * C1;
                        H2 = (H2.RotateLeft(31) + H1) * 5 + 0x38495ab5;
                    }

                    if (remainder > 0)
                        Tail(d + (length - remainder), remainder);
                }
            }
        }

#if NETFX45
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
#endif
        unsafe private void Tail(byte* tail, int remaining)
        {
            // create our keys and initialize to 0
            ulong k1 = 0, k2 = 0;

            // determine how many bytes we have left to work with based on length
            switch (remaining)
            {
                case 15: k2 ^= (ulong)tail[14] << 48; goto case 14;
                case 14: k2 ^= (ulong)tail[13] << 40; goto case 13;
                case 13: k2 ^= (ulong)tail[12] << 32; goto case 12;
                case 12: k2 ^= (ulong)tail[11] << 24; goto case 11;
                case 11: k2 ^= (ulong)tail[10] << 16; goto case 10;
                case 10: k2 ^= (ulong)tail[9] << 8; goto case 9;
                case 9: k2 ^= (ulong)tail[8] << 0; goto case 8;
                case 8: k1 ^= (ulong)tail[7] << 56; goto case 7;
                case 7: k1 ^= (ulong)tail[6] << 48; goto case 6;
                case 6: k1 ^= (ulong)tail[5] << 40; goto case 5;
                case 5: k1 ^= (ulong)tail[4] << 32; goto case 4;
                case 4: k1 ^= (ulong)tail[3] << 24; goto case 3;
                case 3: k1 ^= (ulong)tail[2] << 16; goto case 2;
                case 2: k1 ^= (ulong)tail[1] << 8; goto case 1;
                case 1: k1 ^= (ulong)tail[0] << 0; break;
            }

            H2 ^= (k2 * C2).RotateLeft(33) * C1;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
        }

        byte[] HashFinal()
        {
            ulong len = (ulong)Length;
            H1 ^= len; H2 ^= len;

            H1 += H2;
            H2 += H1;

            H1 = H1.FMix();
            H2 = H2.FMix();

            H1 += H2;
            H2 += H1;

            unsafe
            {
                fixed (byte* h = result)
                {
                    ulong* r = (ulong*)h;

                    r[0] = H1;
                    r[1] = H2;
                }
            }
            return result;
        }



        private void HashCore(int value)
        {
            ulong k1 = (ulong)value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        private void HashCore(uint value)
        {
            ulong k1 = value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        private void HashCore(long value)
        {
            ulong k1 = (ulong)value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        private void HashCore(ulong value)
        {
            ulong k1 = value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        private void HashCore(float value)
        {
            ulong k1 = (ulong)value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        private void HashCore(double value)
        {
            ulong k1 = (ulong)value;
            H1 ^= (k1 * C1).RotateLeft(31) * C2;
            H1 = H1.FMix();
        }

        #endregion

        #region 公共

        /// <summary>
        ///     Computes the 64-bit FNV-1a hash of the given <paramref name="bytes" />, see
        ///     <see cref="http://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function" />
        ///     and <see cref="http://www.isthe.com/chongo/src/fnv/hash_64a.c" />
        /// </summary>
        /// <param name="bytes">Text to compute the hash for</param>
        /// <returns>The 64-bit fnv1a hash</returns>
        public ulong GetHashCode(byte[] bytes)
        {
            Reset();
            HashCore(bytes, 0, bytes.Length);
            HashFinal();
            return H1;
        }

        public ulong GetHashCode(int value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(uint value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(long value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(ulong value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(float value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(double value)
        {
            Reset();
            HashCore(value);
            return H1;
        }

        public ulong GetHashCode(string value)
        {
            unsafe
            {
                fixed (char* pd = value)
                    return GetHashCode(pd, value.Length);
            }
        }

        public ulong GetHashCode(char[] value)
        {
            unsafe
            {
                fixed (char* pd = value)
                    return GetHashCode(pd, value.Length);
            }
        }

        private unsafe ulong GetHashCode(char* value, int count)
        {
            const ulong fnv1A64Init = 14695981039346656037;
            const ulong fnv64Prime = 0x100000001b3;
            ulong hash = fnv1A64Init;
            for (int i = 0; i < count; i++)
            {
                hash ^= *value;
                hash *= fnv64Prime;
                value++;
            }
            return hash;
        }

        #endregion


    }
}
