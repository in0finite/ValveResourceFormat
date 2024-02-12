// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

// code taken from .NET v5.0.18



#nullable enable

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;


namespace System
{
    // Portions of the code implemented below are based on the 'Berkeley SoftFloat Release 3e' algorithms.

    /// <summary>
    /// An IEEE 754 compliant float16 type.
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    public readonly struct Half : IComparable, IComparable<Half>, IEquatable<Half>
    {
        private const NumberStyles DefaultParseStyle = NumberStyles.Float | NumberStyles.AllowThousands;

        // Constants for manipulating the private bit-representation

        private const ushort SignMask = 0x8000;
        private const ushort SignShift = 15;

        private const ushort ExponentMask = 0x7C00;
        private const ushort ExponentShift = 10;

        private const ushort SignificandMask = 0x03FF;
        private const ushort SignificandShift = 0;

        private const ushort MinSign = 0;
        private const ushort MaxSign = 1;

        private const ushort MinExponent = 0x00;
        private const ushort MaxExponent = 0x1F;

        private const ushort MinSignificand = 0x0000;
        private const ushort MaxSignificand = 0x03FF;

        // Constants representing the private bit-representation for various default values

        private const ushort PositiveZeroBits = 0x0000;
        private const ushort NegativeZeroBits = 0x8000;

        private const ushort EpsilonBits = 0x0001;

        private const ushort PositiveInfinityBits = 0x7C00;
        private const ushort NegativeInfinityBits = 0xFC00;

        private const ushort PositiveQNaNBits = 0x7E00;
        private const ushort NegativeQNaNBits = 0xFE00;

        private const ushort MinValueBits = 0xFBFF;
        private const ushort MaxValueBits = 0x7BFF;

        // Well-defined and commonly used values

        public static Half Epsilon => new Half(EpsilonBits);                        //  5.9604645E-08

        public static Half PositiveInfinity => new Half(PositiveInfinityBits);      //  1.0 / 0.0;

        public static Half NegativeInfinity => new Half(NegativeInfinityBits);      // -1.0 / 0.0

        public static Half NaN => new Half(NegativeQNaNBits);                       //  0.0 / 0.0

        public static Half MinValue => new Half(MinValueBits);                      // -65504

        public static Half MaxValue => new Half(MaxValueBits);                      //  65504

        // We use these explicit definitions to avoid the confusion between 0.0 and -0.0.
        private static readonly Half PositiveZero = new Half(PositiveZeroBits);            //  0.0
        private static readonly Half NegativeZero = new Half(NegativeZeroBits);            // -0.0

        private readonly ushort _value;

        internal Half(ushort value)
        {
            _value = value;
        }

        private Half(bool sign, ushort exp, ushort sig)
            => _value = (ushort)(((sign ? 1 : 0) << SignShift) + (exp << ExponentShift) + sig);

        private sbyte Exponent
        {
            get
            {
                return (sbyte)((_value & ExponentMask) >> ExponentShift);
            }
        }

        private ushort Significand
        {
            get
            {
                return (ushort)((_value & SignificandMask) >> SignificandShift);
            }
        }

        public static bool operator <(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative && !AreZero(left, right);
            }
            return (left._value < right._value) ^ leftIsNegative;
        }

        public static bool operator >(Half left, Half right)
        {
            return right < left;
        }

        public static bool operator <=(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is unordered with respect to everything, including itself.
                return false;
            }

            bool leftIsNegative = IsNegative(left);

            if (leftIsNegative != IsNegative(right))
            {
                // When the signs of left and right differ, we know that left is less than right if it is
                // the negative value. The exception to this is if both values are zero, in which case IEEE
                // says they should be equal, even if the signs differ.
                return leftIsNegative || AreZero(left, right);
            }
            return (left._value <= right._value) ^ leftIsNegative;
        }

        public static bool operator >=(Half left, Half right)
        {
            return right <= left;
        }

        public static bool operator ==(Half left, Half right)
        {
            if (IsNaN(left) || IsNaN(right))
            {
                // IEEE defines that NaN is not equal to anything, including itself.
                return false;
            }

            // IEEE defines that positive and negative zero are equivalent.
            return (left._value == right._value) || AreZero(left, right);
        }

        public static bool operator !=(Half left, Half right)
        {
            return !(left == right);
        }

        /// <summary>Determines whether the specified value is finite (zero, subnormal, or normal).</summary>
        public static bool IsFinite(Half value)
        {
            return StripSign(value) < PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is infinite.</summary>
        public static bool IsInfinity(Half value)
        {
            return StripSign(value) == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is NaN.</summary>
        public static bool IsNaN(Half value)
        {
            return StripSign(value) > PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is negative.</summary>
        public static bool IsNegative(Half value)
        {
            return (short)(value._value) < 0;
        }

        /// <summary>Determines whether the specified value is negative infinity.</summary>
        public static bool IsNegativeInfinity(Half value)
        {
            return value._value == NegativeInfinityBits;
        }

        /// <summary>Determines whether the specified value is normal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsNormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & ExponentMask) != 0);    // is not subnormal (has a non-zero exponent)
        }

        /// <summary>Determines whether the specified value is positive infinity.</summary>
        public static bool IsPositiveInfinity(Half value)
        {
            return value._value == PositiveInfinityBits;
        }

        /// <summary>Determines whether the specified value is subnormal.</summary>
        // This is probably not worth inlining, it has branches and should be rarely called
        public static bool IsSubnormal(Half value)
        {
            uint absValue = StripSign(value);
            return (absValue < PositiveInfinityBits)    // is finite
                && (absValue != 0)                      // is not zero
                && ((absValue & ExponentMask) == 0);    // is subnormal (has a zero exponent)
        }

        private static bool AreZero(Half left, Half right)
        {
            // IEEE defines that positive and negative zero are equal, this gives us a quick equality check
            // for two values by or'ing the private bits together and stripping the sign. They are both zero,
            // and therefore equivalent, if the resulting value is still zero.
            return (ushort)((left._value | right._value) & ~SignMask) == 0;
        }

        private static bool IsNaNOrZero(Half value)
        {
            return ((value._value - 1) & ~SignMask) >= PositiveInfinityBits;
        }

        private static uint StripSign(Half value)
        {
            return (ushort)(value._value & ~SignMask);
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="obj"/>, zero if this is equal to <paramref name="obj"/>, or a value greater than zero if this is greater than <paramref name="obj"/>.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="obj"/> is not of type <see cref="Half"/>.</exception>
        public int CompareTo(object? obj)
        {
            if (!(obj is Half))
            {
                return (obj is null) ? 1 : throw new ArgumentException();
            }
            return CompareTo((Half)(obj));
        }

        /// <summary>
        /// Compares this object to another object, returning an integer that indicates the relationship.
        /// </summary>
        /// <returns>A value less than zero if this is less than <paramref name="other"/>, zero if this is equal to <paramref name="other"/>, or a value greater than zero if this is greater than <paramref name="other"/>.</returns>
        public int CompareTo(Half other)
        {
            if (this < other)
            {
                return -1;
            }

            if (this > other)
            {
                return 1;
            }

            if (this == other)
            {
                return 0;
            }

            if (IsNaN(this))
            {
                return IsNaN(other) ? 0 : -1;
            }

            Debug.Assert(IsNaN(other));
            return 1;
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="obj"/>.
        /// </summary>
        public override bool Equals(object? obj)
        {
            return (obj is Half other) && Equals(other);
        }

        /// <summary>
        /// Returns a value that indicates whether this instance is equal to a specified <paramref name="other"/> value.
        /// </summary>
        public bool Equals(Half other)
        {
            if (this == other)
            {
                return true;
            }
            return IsNaN(this) && IsNaN(other);
        }

        /// <summary>
        /// Serves as the default hash function.
        /// </summary>
        public override int GetHashCode()
        {
            if (IsNaNOrZero(this))
            {
                // All NaNs should have the same hash code, as should both Zeros.
                return _value & PositiveInfinityBits;
            }
            return _value;
        }

        // -----------------------Start of to-half conversions-------------------------

        internal const uint FloatSignMask = 0x8000_0000;
        internal const int FloatSignShift = 31;
        internal const int FloatShiftedSignMask = (int)(FloatSignMask >> FloatSignShift);

        internal const uint FloatExponentMask = 0x7F80_0000;
        internal const int FloatExponentShift = 23;
        internal const int FloatShiftedExponentMask = (int)(FloatExponentMask >> FloatExponentShift);

        internal const uint FloatSignificandMask = 0x007F_FFFF;

        public static explicit operator Half(float value)
        {
            const int SingleMaxExponent = 0xFF;

            uint floatInt = (uint)BitConverter.SingleToInt32Bits(value);
            bool sign = (floatInt & FloatSignMask) >> FloatSignShift != 0;
            int exp = (int)(floatInt & FloatExponentMask) >> FloatExponentShift;
            uint sig = floatInt & FloatSignificandMask;

            if (exp == SingleMaxExponent)
            {
                if (sig != 0) // NaN
                {
                    return CreateHalfNaN(sign, (ulong)sig << 41); // Shift the significand bits to the left end
                }
                return sign ? NegativeInfinity : PositiveInfinity;
            }

            uint sigHalf = sig >> 9 | ((sig & 0x1FFU) != 0 ? 1U : 0U); // RightShiftJam

            if ((exp | (int)sigHalf) == 0)
            {
                return new Half(sign, 0, 0);
            }

            return new Half(RoundPackToHalf(sign, (short)(exp - 0x71), (ushort)(sigHalf | 0x4000)));
        }

        // -----------------------Start of from-half conversions-------------------------
        public static explicit operator float(Half value)
        {
            bool sign = IsNegative(value);
            int exp = value.Exponent;
            uint sig = value.Significand;

            if (exp == MaxExponent)
            {
                if (sig != 0)
                {
                    return CreateSingleNaN(sign, (ulong)sig << 54);
                }
                return sign ? float.NegativeInfinity : float.PositiveInfinity;
            }

            if (exp == 0)
            {
                if (sig == 0)
                {
                    return BitConverter.Int32BitsToSingle((int)(sign ? FloatSignMask : 0)); // Positive / Negative zero
                }
                (exp, sig) = NormSubnormalF16Sig(sig);
                exp -= 1;
            }

            return CreateSingle(sign, (byte)(exp + 0x70), sig << 13);
        }

        // IEEE 754 specifies NaNs to be propagated
        internal static Half Negate(Half value)
        {
            return IsNaN(value) ? value : new Half((ushort)(value._value ^ SignMask));
        }

        private static (int Exp, uint Sig) NormSubnormalF16Sig(uint sig)
        {
            int shiftDist = BitOperations.LeadingZeroCount(sig) - 16 - 5;
            return (1 - shiftDist, sig << shiftDist);
        }

        #region Utilities

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe Half Int16BitsToHalf(short value)
        {
            return *(Half*)&value;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static unsafe short HalfToInt16Bits(Half value)
        {
            return *((short*)&value);
        }

        // Significand bits should be shifted towards to the left end before calling these methods
        // Creates Quiet NaN if significand == 0
        private static Half CreateHalfNaN(bool sign, ulong significand)
        {
            const uint NaNBits = ExponentMask | 0x200; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << SignShift;
            uint sigInt = (uint)(significand >> 54);

            return Int16BitsToHalf((short)(signInt | NaNBits | sigInt));
        }

        private static ushort RoundPackToHalf(bool sign, short exp, ushort sig)
        {
            const int RoundIncrement = 0x8; // Depends on rounding mode but it's always towards closest / ties to even
            int roundBits = sig & 0xF;

            if ((uint)exp >= 0x1D)
            {
                if (exp < 0)
                {
                    sig = (ushort)ShiftRightJam(sig, -exp);
                    exp = 0;
                    roundBits = sig & 0xF;
                }
                else if (exp > 0x1D || sig + RoundIncrement >= 0x8000) // Overflow
                {
                    return sign ? NegativeInfinityBits : PositiveInfinityBits;
                }
            }

            sig = (ushort)((sig + RoundIncrement) >> 4);
            sig &= (ushort)~(((roundBits ^ 8) != 0 ? 0 : 1) & 1);

            if (sig == 0)
            {
                exp = 0;
            }

            return new Half(sign, (ushort)exp, sig)._value;
        }

        // If any bits are lost by shifting, "jam" them into the LSB.
        // if dist > bit count, Will be 1 or 0 depending on i
        // (unlike bitwise operators that masks the lower 5 bits)
        private static uint ShiftRightJam(uint i, int dist)
            => dist < 31 ? (i >> dist) | (i << (-dist & 31) != 0 ? 1U : 0U) : (i != 0 ? 1U : 0U);

        private static ulong ShiftRightJam(ulong l, int dist)
            => dist < 63 ? (l >> dist) | (l << (-dist & 63) != 0 ? 1UL : 0UL) : (l != 0 ? 1UL : 0UL);

        private static float CreateSingleNaN(bool sign, ulong significand)
        {
            const uint NaNBits = FloatExponentMask | 0x400000; // Most significant significand bit

            uint signInt = (sign ? 1U : 0U) << FloatSignShift;
            uint sigInt = (uint)(significand >> 41);

            return BitConverter.Int32BitsToSingle((int)(signInt | NaNBits | sigInt));
        }

        private static float CreateSingle(bool sign, byte exp, uint sig)
            => BitConverter.Int32BitsToSingle((int)(((sign ? 1U : 0U) << FloatSignShift) + ((uint)exp << FloatExponentShift) + sig));

        #endregion
    }
}
