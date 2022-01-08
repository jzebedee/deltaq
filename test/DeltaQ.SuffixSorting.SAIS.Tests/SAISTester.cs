using DeltaQ.SuffixSorting;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Xunit;

namespace DeltaQ.Tests;

using static SAISChecker;
public abstract class SAISTester
{
    private const string FuzzFilesBasePath = "assets/";

    protected abstract ISuffixSort Impl { get; }

    private static SpanOwner<byte> GetOwnedRandomBuffer(int size)
    {
        var rand = new Random(63 * 13 * 63 * 13);

        var owner = SpanOwner<byte>.Allocate(size);
#if NETFRAMEWORK
            var buf = new byte[size];
            rand.NextBytes(buf);
            buf.CopyTo(owner.Span);
#else
        rand.NextBytes(owner.Span);
#endif

        return owner;
    }

    public static IEnumerable<object[]> Sizes { get; } = (new[] { 0, 1, 2, 4, 8, 16, 32, 51, 0x8000 - 1, 0x8000 })
        .Select(size => new object[] { size })
        .ToArray();

    [Theory]
    [MemberData(nameof(Sizes))]
    public void CheckRandomBuffer(int size)
    {
        using var ownedT = GetOwnedRandomBuffer(size);
        Span<byte> T = ownedT.Span;

        using var ownedSA = Impl.Sort(T);
        Span<int> SA = ownedSA.Memory.Span;

        var result = Check(T, SA, T.Length, false);
        Assert.Equal(0, result);
    }

    public static IEnumerable<object[]> FuzzFiles { get; } = Directory.EnumerateFiles(FuzzFilesBasePath)
        .Select(file => new object[] { file })
        .ToArray();

    [Theory]
    [MemberData(nameof(FuzzFiles))]
    public void CheckFile(string path)
    {
        var bytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> T = bytes;

        using var ownedSA = Impl.Sort(T);
        Span<int> SA = ownedSA.Memory.Span;

        var result = Check(T, SA, T.Length, false);
        Assert.Equal(0, result);
    }
}
