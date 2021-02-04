using System;

namespace DeltaQ.BsDiff
{
    internal static class ArraySegmentExtensions
    {
        public static ArraySegment<T> Slice<T>(this T[] buf, int offset, int count = -1)
        {
            //substitute everything remaining after the offset, if count is subzero
            return new ArraySegment<T>(buf, offset, count < 0 ? buf.Length - offset : count);
        }
    }
}
