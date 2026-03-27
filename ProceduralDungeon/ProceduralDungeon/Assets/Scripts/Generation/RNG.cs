using System;
using UnityEngine;

namespace Generation
{
    public static class RNG
    {
        private static readonly char[] Alphabet = "0123456789ABCDEF".ToCharArray();
        
        public static string MutateNext(string originalSeed)
        {
            var seed = ParseSeed(originalSeed);
            return ParseSeed(MutateNext(seed));
        }

        private static ulong MutateNext(ulong originalSeed)
        {
            const ulong BIT_NOISE1 = 0xB5297A4DB5297A4D;
            const ulong BIT_NOISE2 = 0x68E31DA468E31DA4;
            const ulong BIT_NOISE3 = 0x1B56C4E91B56C4E9;
            
            var result = originalSeed;
            result *= BIT_NOISE1;
            result ^= (result >> 8);
            result *= BIT_NOISE2;
            result ^= (result >> 8);
            result *= BIT_NOISE3;
            result ^= (result >> 8);
            
            return result;
        }

        /// <summary>
        /// converts an uint64 seed, into a string
        /// </summary>
        /// <returns>Seed as a string</returns>
        public static string ParseSeed(ulong seed)
        {
            var chars = new char[16]; // amount of characters we're getting from the uint64
            
            // Fill from right to left
            for (var i = 15; i >= 0; i--)
            {
                var index = (int)(seed & 0xF); // using a bitmask extract needed bits, as index
                chars[i] = Alphabet[index];
                seed >>= 4; // shift by char size
            }
            
            return new string(chars);
        }
        
        /// <summary>
        /// converts a string seed into an uint64 seed
        /// </summary>
        /// <returns>seed as an uint64</returns>
        public static ulong ParseSeed(string seed)
        {
            ulong result = 0;
            
            // read from left to right
            for (var i = 0; i < Math.Min(seed.Length, 16); i++)
            {
                var value = GetHexValue(seed[i]);
                result = (result << 4) | (uint)value; // shift result to left adding more zeros, then add found value to result
            }
            
            return result;
        }

        /// <summary>
        /// Quicker than indexOf, manually convert the char to a int,
        /// fallback at the end bringing char's back to our 16 char array
        /// </summary>
        private static int GetHexValue(char c)
        {
            if (c >= '0' && c <= '9') return c - '0';
            if (c >= 'A' && c <= 'F') return c - 'A' + 10;
            if (c >= 'a' && c <= 'f') return c - 'a' + 10;
            return c % 16;
        }

        public static int RandomRange(int min, int max, string randomSeed)
        {
            var seed = ParseSeed(randomSeed);
            var normalized = (double)seed / ulong.MaxValue;
            return (int)(normalized * (max - min)) + min;
        }
    }
}