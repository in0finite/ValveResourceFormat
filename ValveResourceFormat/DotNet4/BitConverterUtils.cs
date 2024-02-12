namespace ValveResourceFormat
{
    internal class BitConverterUtils
    {
        public static Half ToHalf(ReadOnlySpan<byte> span)
        {
            short i16 = BitConverter.ToInt16(span);
            return Half.Int16BitsToHalf(i16);
        }

        public static Half ToHalf(byte[] a, int offset)
        {
            short i16 = BitConverter.ToInt16(a, offset);
            return Half.Int16BitsToHalf(i16);
        }

        public static bool TryWriteBytes(Span<byte> span, Half half)
        {
            return BitConverter.TryWriteBytes(span, Half.HalfToInt16Bits(half));
        }
    }
}
