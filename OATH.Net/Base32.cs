﻿//------------------------------------------------------------------------------------
// <copyright file="Base32.cs" company="Stephen Jennings">
//   Copyright 2011 Stephen Jennings. Licensed under the Apache License, Version 2.0.
// </copyright>
//------------------------------------------------------------------------------------

namespace OathNet
{
    using System;
    using System.Collections;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;

    /// <summary>
    ///     Contains methods to convert to and from base-32 according to RFC 3548.
    /// </summary>
    public static class Base32
    {
        private static readonly string[] Alphabet = new string[]
        {
            "A", "B", "C", "D", "E", "F", "G", "H", "I", "J",
            "K", "L", "M", "N", "O", "P", "Q", "R", "S", "T",
            "U", "V", "W", "X", "Y", "Z", "2", "3", "4", "5",
            "6", "7"
        };

        private static readonly Dictionary<char, byte> AlphabetReverse = new Dictionary<char, byte>
        {
            { 'A', 0 }, { 'B', 1 }, { 'C', 2 }, { 'D', 3 }, { 'E', 4 }, { 'F', 5 }, { 'G', 6 }, { 'H', 7 }, { 'I', 8 }, { 'J', 9 },
            { 'K', 10 }, { 'L', 11 }, { 'M', 12 }, { 'N', 13 }, { 'O', 14 }, { 'P', 15 }, { 'Q', 16 }, { 'R', 17 }, { 'S', 18 }, { 'T', 19 },
            { 'U', 20 }, { 'V', 21 }, { 'W', 22 }, { 'X', 23 }, { 'Y', 24 }, { 'Z', 25 }, { '2', 26 }, { '3', 27 }, { '4', 28 }, { '5', 29 },
            { '6', 30 }, { '7', 31 }
        };

        private static readonly char Padding = '=';

        /// <summary>
        ///     Converts a byte array to a base-32 representation.
        /// </summary>
        /// <param name="data">The data to convert.</param>
        /// <returns>A base-32 encoded string.</returns>
        public static string ToBase32(byte[] data)
        {
            string result = String.Empty;

            var fullSegments = data.Length / 5;
            var finalSegmentLength = data.Length % 5;
            var segments = fullSegments + (finalSegmentLength == 0 ? 0 : 1);

            for (int i = 0; i < segments; i++)
            {
                var segment = data.Skip(i * 5).Take(5).ToArray();
                result = String.Concat(result, Base32.ConvertSegmentToBase32(segment));
            }

            return result;
        }

        /// <summary>
        ///     Converts a base-32 encoded string to a byte array.
        /// </summary>
        /// <param name="base32">A base-32 encoded string.</param>
        /// <returns>The data represented by the base-32 string.</returns>
        public static byte[] ToBinary(string base32)
        {
            base32 = base32.ToUpper();

            if (base32.Any(c => !AlphabetReverse.ContainsKey(c) && c != Padding))
            {
                throw new ArgumentException("String contains invalid characters.");
            }

            var fullSegments = base32.Length / 8;
            var finalSegmentLength = base32.Length % 8;
            var segments = fullSegments + (finalSegmentLength == 0 ? 0 : 1);

            IEnumerable<byte> result = new byte[0];

            for (int i = 0; i < segments; i++)
            {
                var segment = base32.Skip(i * 8).Take(8).ToArray();
                var slice = Base32.ConvertSegmentToBinary(segment);
                result = result.Concat(slice);
            }

            return result.ToArray();
        }

        private static byte[] ConvertSegmentToBinary(char[] segment)
        {
            if (segment.Length != 8)
            {
                throw new ArgumentException("Segment must be 8 characters in length.");
            }

            byte[] result = new byte[5];
            var s = segment;
            var bytesCreated = 0;
            
            try
            {
                result[0] = (byte)(AlphabetReverse[s[0]] << 3 | AlphabetReverse[s[1]] >> 2);
                bytesCreated = 1;
                result[1] = (byte)(AlphabetReverse[s[1]] << 6 | AlphabetReverse[s[2]] << 1 | AlphabetReverse[s[3]] >> 4);
                bytesCreated = 2;
                result[2] = (byte)(AlphabetReverse[s[3]] << 4 | AlphabetReverse[s[4]] >> 1);
                bytesCreated = 3;
                result[3] = (byte)(AlphabetReverse[s[4]] << 7 | AlphabetReverse[s[5]] << 2 | AlphabetReverse[s[6]] >> 3);
                bytesCreated = 4;
                result[4] = (byte)(AlphabetReverse[s[6]] << 5 | AlphabetReverse[s[7]]);
                bytesCreated = 5;
            }
            catch (KeyNotFoundException)
            {
                Array.Resize(ref result, bytesCreated);
            }

            return result;
        }

        private static string ConvertSegmentToBase32(byte[] segment)
        {
            if (segment.Length == 0)
            {
                return String.Empty;
            }

            if (segment.Length > 5)
            {
                throw new ArgumentException("Segment must be five bytes or fewer.");
            }

            string result = String.Empty;

            int accumulator = 0;
            int bitsRemaining = 5;
            byte[] masks = new byte[] { 0x00, 0x01, 0x03, 0x07, 0x0F, 0x1F };

            foreach (var b in segment)
            {
                // Accumulate the bits remaining from the previous byte, if any
                int bottomBitsInThisByte = 8 - bitsRemaining;
                accumulator += (b >> bottomBitsInThisByte) & masks[Math.Min(bitsRemaining, 5)];

                // Add the accumulated character to the result string
                result = result + Alphabet[accumulator];

                if (bottomBitsInThisByte >= 5)
                {
                    bottomBitsInThisByte -= 5;

                    // Set the accumulator to the next 5 bits in this byte
                    accumulator = (b >> bottomBitsInThisByte) & masks[5];

                    // Add the accumulated character to the result string
                    result = result + Alphabet[accumulator];
                }

                // Decide how many more bits we need to accumulate from the next byte
                bitsRemaining = (5 - bottomBitsInThisByte) % 5;

                // Set the accumulator to the remaining bits in this byte
                accumulator = (b & masks[bottomBitsInThisByte]) << bitsRemaining;
            }

            if (bitsRemaining > 0)
            {
                // Capture the final accumulated value
                result = result + Alphabet[accumulator];
            }

            result = result.PadRight(8, Padding);

            return result;
        }
    }
}