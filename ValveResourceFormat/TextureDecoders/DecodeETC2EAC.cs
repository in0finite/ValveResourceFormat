// Credit to https://github.com/mafaca/Etc

using System;
using System.Runtime.CompilerServices;
using SkiaSharp;

namespace ValveResourceFormat.TextureDecoders
{
    internal class DecodeETC2EAC : CommonETC, ITextureDecoder
    {
        private static readonly byte[] WriteOrderTableRev = { 15, 11, 7, 3, 14, 10, 6, 2, 13, 9, 5, 1, 12, 8, 4, 0 };
        private static readonly sbyte[,] Etc2AlphaModTable =
        {
            {-3, -6,  -9, -15, 2, 5, 8, 14},
            {-3, -7, -10, -13, 2, 6, 9, 12},
            {-2, -5,  -8, -13, 1, 4, 7, 12},
            {-2, -4,  -6, -13, 1, 3, 5, 12},
            {-3, -6,  -8, -12, 2, 5, 7, 11},
            {-3, -7,  -9, -11, 2, 6, 8, 10},
            {-4, -7,  -8, -11, 3, 6, 7, 10},
            {-3, -5,  -8, -11, 2, 4, 7, 10},
            {-2, -6,  -8, -10, 1, 5, 7,  9},
            {-2, -5,  -8, -10, 1, 4, 7,  9},
            {-2, -4,  -8, -10, 1, 3, 7,  9},
            {-2, -5,  -7, -10, 1, 4, 6,  9},
            {-3, -4,  -7, -10, 2, 3, 6,  9},
            {-1, -2,  -3, -10, 0, 1, 2,  9},
            {-4, -6,  -8,  -9, 3, 5, 7,  8},
            {-3, -5,  -7,  -9, 2, 4, 6,  8}
        };

        readonly int width;
        readonly int height;

        public DecodeETC2EAC(int width, int height)
        {
            this.width = width;
            this.height = height;
        }

        public void Decode(SKBitmap res, Span<byte> input)
        {
            using var pixels = res.PeekPixels();
            var output = pixels.GetPixelSpan<byte>();

            int bcw = (width + 3) / 4;
            int bch = (height + 3) / 4;
            int clen_last = (width + 3) % 4 + 1;
            int d = 0;

            for (int t = 0; t < bch; t++)
            {
                for (int s = 0; s < bcw; s++, d += 16)
                {
                    DecodeEtc2Block(input, d + 8);
                    DecodeEtc2a8Block(input, d);
                    int clen = (s < bcw - 1 ? 4 : clen_last) * 4;
                    for (int i = 0, y = t * 4; i < 4 && y < height; i++, y++)
                    {
                        // TODO: This is rather silly
                        var bytes = new byte[clen];
                        Buffer.BlockCopy(m_buf, i * 4 * 4, bytes, 0, clen);
                        bytes.CopyTo(output.Slice(y * 4 * width + s * 4 * 4, clen));
                    }
                }
            }
        }

        private void DecodeEtc2a8Block(Span<byte> data, int offset)
        {
            int @base = data[offset + 0];
            int data1 = data[offset + 1];
            int mul = data1 >> 4;
            if (mul == 0)
            {
                for (int i = 0; i < 16; i++)
                {
                    uint c = m_buf[WriteOrderTableRev[i]];
                    c &= 0x00FFFFFF;
                    c |= unchecked((uint)(@base << 24));
                    m_buf[WriteOrderTableRev[i]] = c;
                }
            }
            else
            {
                int table = data1 & 0xF;
                ulong l = Get6SwapedBytes(data, offset);
                for (int i = 0; i < 16; i++, l >>= 3)
                {
                    uint c = m_buf[WriteOrderTableRev[i]];
                    c &= 0x00FFFFFF;
                    c |= unchecked((uint)(Clamp255(@base + mul * Etc2AlphaModTable[table, l & 7]) << 24));
                    m_buf[WriteOrderTableRev[i]] = c;
                }
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ulong Get6SwapedBytes(Span<byte> data, int offset)
        {
            return data[offset + 7] | (uint)data[offset + 6] << 8 |
                    (uint)data[offset + 5] << 16 | (uint)data[offset + 4] << 24 |
                    (ulong)data[offset + 3] << 32 | (ulong)data[offset + 2] << 40;
        }
    }
}