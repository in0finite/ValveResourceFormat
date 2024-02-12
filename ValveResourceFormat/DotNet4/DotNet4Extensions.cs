using System.Text;

namespace ValveResourceFormat
{
    public static class DotNet4Extensions
    {
        public const float Tau = 6.28318548f;

        public static StringBuilder AppendLine(this StringBuilder sb, IFormatProvider formatProvider, string value)
        {
            return sb.AppendLine(value);
        }

        public static StringBuilder Append(this StringBuilder sb, IFormatProvider formatProvider, string value)
        {
            return sb.Append(value);
        }

        public static int EnsureCapacity<T>(this List<T> list, int capacity)
        {
            if (list.Capacity < capacity)
                list.Capacity = capacity;
            return list.Capacity;
        }

        public static void ThrowIfNull(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException();
            }
        }

        // taken from .NET 8 source code
        public static void ThrowIfLessThan<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) < 0)
                throw new ArgumentOutOfRangeException(paramName);
        }

        public static void ThrowIfGreaterThanOrEqual<T>(T value, T other, [CallerArgumentExpression(nameof(value))] string paramName = null)
            where T : IComparable<T>
        {
            if (value.CompareTo(other) >= 0)
                throw new ArgumentOutOfRangeException(paramName);
        }

        public static Vector4 Vec4FromSpan(ReadOnlySpan<float> span)
        {
            if (span.Length < 4)
                throw new ArgumentException("Span must contain at least 4 elements");
            return new Vector4(span[0], span[1], span[2], span[3]);
        }

        public static Vector3 Vec3FromSpan(ReadOnlySpan<float> span)
        {
            if (span.Length < 3)
                throw new ArgumentException("Span must contain at least 3 elements");
            return new Vector3(span[0], span[1], span[2]);
        }

        // taken from .NET 8 source code
        public static float FloatLerp(float value1, float value2, float amount) => (value1 * (1.0f - amount)) + (value2 * amount);

        // taken from .NET 8 source code
        static bool IsCharBetween(char c, char minInclusive, char maxInclusive) =>
            (uint)(c - minInclusive) <= (uint)(maxInclusive - minInclusive);

        public static bool IsAsciiDigit(char c) => IsCharBetween(c, '0', '9');

        public static bool IsAsciiLetter(char c) => (uint)((c | 0x20) - 'a') <= 'z' - 'a';

        public static bool IsAsciiLetterOrDigit(char c) => IsAsciiLetter(c) | IsCharBetween(c, '0', '9');
    }
}
