//#define ISQRT_LOOKUP
//#define ILOG2_LOOKUP
using System;
using System.Runtime.CompilerServices;
using System.Numerics;
using Idx = System.Int32;

namespace DeltaQ.SuffixSorting.LibDivSufSort;

internal static class Utils
{
#if ILOG2_LOOKUP
    private static readonly int[] lg_table_array = new[]
    {
         -1,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,
          5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,
          6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
          6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
          7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,
    };
    internal static ReadOnlySpan<int> lg_table => lg_table_array;
#endif

#if !ILOG2LOOKUP && !NETCOREAPP3_0_OR_GREATER
    public static int FastLog2(int n)
    {
        int r = 0xFFFF - n >> 31 & 0x10;
        n >>= r;
        int shift = 0xFF - n >> 31 & 0x8;
        n >>= shift;
        r |= shift;
        shift = 0xF - n >> 31 & 0x4;
        n >>= shift;
        r |= shift;
        shift = 0x3 - n >> 31 & 0x2;
        n >>= shift;
        r |= shift;
        r |= (n >> 1);
        return r;
    }
#endif

    /// <summary>
    /// Fast log2, using lookup tables
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    //allow unchecked
    public static int tr_ilg(int n)
#if ILOG2_LOOKUP
    {
        if ((n & 0xffff_0000) > 0)
        {
            if ((n & 0xff00_0000) > 0)
            {
                return 24 + lg_table[((n >> 24) & 0xff)];
            }
            else
            {
                return 16 + lg_table[((n >> 16) & 0xff)];
            }
        }
        else
        {
            if ((n & 0x0000_ff00) > 0)
            {
                return 8 + lg_table[((n >> 8) & 0xff)];
            }
            else
            {
                return 0 + lg_table[((n >> 0) & 0xff)];
            }
        }
    }
#elif NETCOREAPP3_0_OR_GREATER
        => BitOperations.Log2((uint)n);
#else
        => FastLog2(n);
#endif

    /// <summary>
    /// Fast log2, using lookup tables
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static int ss_ilg(int n)
#if ILOG2_LOOKUP
        => n & 0xff00 switch
        {
            > 0 => 8 + lg_table[n >> 8 & 0xff],
              _ => 0 + lg_table[n >> 0 & 0xff]
        };
#elif NETCOREAPP3_0_OR_GREATER
        => BitOperations.Log2((uint)n);
#else
        => FastLog2(n);
#endif

#if ISQRT_LOOKUP
    private static readonly Idx[] sqq_table_array = new[]
    {
          0,  16,  22,  27,  32,  35,  39,  42,  45,  48,  50,  53,  55,  57,  59,  61,
         64,  65,  67,  69,  71,  73,  75,  76,  78,  80,  81,  83,  84,  86,  87,  89,
         90,  91,  93,  94,  96,  97,  98,  99, 101, 102, 103, 104, 106, 107, 108, 109,
        110, 112, 113, 114, 115, 116, 117, 118, 119, 120, 121, 122, 123, 124, 125, 126,
        128, 128, 129, 130, 131, 132, 133, 134, 135, 136, 137, 138, 139, 140, 141, 142,
        143, 144, 144, 145, 146, 147, 148, 149, 150, 150, 151, 152, 153, 154, 155, 155,
        156, 157, 158, 159, 160, 160, 161, 162, 163, 163, 164, 165, 166, 167, 167, 168,
        169, 170, 170, 171, 172, 173, 173, 174, 175, 176, 176, 177, 178, 178, 179, 180,
        181, 181, 182, 183, 183, 184, 185, 185, 186, 187, 187, 188, 189, 189, 190, 191,
        192, 192, 193, 193, 194, 195, 195, 196, 197, 197, 198, 199, 199, 200, 201, 201,
        202, 203, 203, 204, 204, 205, 206, 206, 207, 208, 208, 209, 209, 210, 211, 211,
        212, 212, 213, 214, 214, 215, 215, 216, 217, 217, 218, 218, 219, 219, 220, 221,
        221, 222, 222, 223, 224, 224, 225, 225, 226, 226, 227, 227, 228, 229, 229, 230,
        230, 231, 231, 232, 232, 233, 234, 234, 235, 235, 236, 236, 237, 237, 238, 238,
        239, 240, 240, 241, 241, 242, 242, 243, 243, 244, 244, 245, 245, 246, 246, 247,
        247, 248, 248, 249, 249, 250, 250, 251, 251, 252, 252, 253, 253, 254, 254, 255
    };
    internal static ReadOnlySpan<Idx> sqq_table => sqq_table_array;
#endif

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    internal static void Swap<T>(ref T lhs, ref T rhs)
    {
        T temp;
        temp = lhs;
        lhs = rhs;
        rhs = temp;
    }
}