using System;
using System.Buffers;

namespace DeltaQ.SuffixSorting;

/// <summary>
/// Provides functionality to sort inputs into a suffix array representation.
/// </summary>
public interface ISuffixSort
{
    /// <summary>
    /// Sort a buffer <paramref name="text"/> and return a new 
    /// <c cref="System.Buffers.IMemoryOwner{int}">IMemoryOwner&lt;int&gt;</c> containing
    /// suffixes of <paramref name="text"/> in sorted lexicographical order.
    /// </summary>
    /// <param name="text">Buffer containing text to sort.</param>
    /// <returns><c cref="System.Buffers.IMemoryOwner{int}">IMemoryOwner&lt;int&gt;</c> containing suffixes of <paramref name="text"/> in sorted lexicographical order.</returns>
    IMemoryOwner<int> Sort(ReadOnlySpan<byte> text);
    /// <summary>
    /// Sort a buffer <paramref name="text"/> and fill a result buffer
    /// <paramref name="suffixes"/> with suffixes of
    /// <paramref name="text"/> in sorted lexicographical order.
    /// </summary>
    /// <remarks>The <paramref name="text"/> buffer and <paramref name="suffixes"/> buffer should be the same length.</remarks>
    /// <param name="text">Buffer containing text to sort.</param>
    /// <param name="suffixes">Buffer containing suffixes of <paramref name="text"/> in sorted lexicographical order.</param>
    void Sort(ReadOnlySpan<byte> text, Span<int> suffixes);
}