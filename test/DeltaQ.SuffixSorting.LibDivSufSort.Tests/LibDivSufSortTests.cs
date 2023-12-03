using CommunityToolkit.HighPerformance.Buffers;
using DeltaQ.SuffixSorting.LibDivSufSort;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaQ.Tests;

using static Crosscheck;

public class LibDivSufSortTests : IDisposable
{
    private const string FuzzFilesBasePath = "assets/";

    public LibDivSufSortTests()
    {
        SetupCrosscheckListeners();
    }
    public void Dispose()
    {
        FinalizeCrosscheck();
    }

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

    private static void Verify(ReadOnlySpan<byte> T, ReadOnlySpan<int> SA)
    {
        //ref byte suff(int index) => ref input[sa[index]];
        for (int i = 0; i < T.Length - 1; i++)
        {
            //if(!(suff(i) < suff(i + 1)))
            var cur = T[SA[i]..];
            var next = T[SA[i + 1]..];
            var cmp = cur.SequenceCompareTo(next);
            if (!(cmp < 0))
            {
                var ex = new InvalidOperationException("Input was unsorted");
                ex.Data["i"] = i;
                ex.Data["j"] = i + 1;
                throw ex;
            }
        }

        const LDSSChecker.ResultCode expected = LDSSChecker.ResultCode.Done;
        var actual = LDSSChecker.Check(T, SA, true);
        Assert.Equal(expected, actual);
    }

    [Fact]
    public void CheckShruggy()
    {
        const string shruggy = @"¯\_(ツ)_/¯";

        ReadOnlySpan<byte> T = Encoding.UTF8.GetBytes(shruggy);

        var ldss = new LibDivSufSort();
        using var owner = ldss.Sort(T);
        var SA = owner.Memory.Span;
        Verify(T, SA);
    }

    public static IEnumerable<object[]> FuzzFiles
        => FuzzFilesInner.Select(fuzzFile => new object[] {
#if NETCOREAPP3_1_OR_GREATER
            Path.Join(FuzzFilesBasePath, fuzzFile)
#else
            Path.Combine(FuzzFilesBasePath, fuzzFile)
#endif
        });
    private static IEnumerable<string> FuzzFilesInner
    {
        get
        {
            //Crosscheck passed:
            yield return "fuzz1";
            yield return "fuzz2";
            yield return "fuzz3";
            yield return "crash-cf8673530fdca659e0ddf070b4718b9c0bb504ec";
            yield return "crash-ce407adf7cf638d3fa89b5637a94355d7d658872";
            yield return "crash-c792e788de61771b6cd65c1aa5670c62e57a33c4";
            yield return "crash-90b42d1c55ee90a8b004fb9db1853429ceb4c4ba";
            yield return "crash-8765ef2258178ca027876eab83e01d6d58db9ca0";
            yield return "crash-4f8c31dec8c3678a07e0fbacc6bd69e7cc9037fb";
            yield return "crash-16356e91966a827f79e49167170194fc3088a7ab";
            //Crosscheck untested:
            yield return "crash-aoob-ss_mintrosort";
            //yield return "";
        }
    }

    [Theory]
    [MemberData(nameof(FuzzFiles))]
    //[InlineData(@"")]
    public void CheckFile(string path)
    {
        SetupCrosscheckListeners();

        var bytes = File.ReadAllBytes(path);
        ReadOnlySpan<byte> T = bytes;

        var ldss = new LibDivSufSort();
        using var owner = ldss.Sort(T);
        var SA = owner.Memory.Span;
        Verify(T, SA);

        FinalizeCrosscheck();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(4)]
    [InlineData(8)]
    [InlineData(16)]
    [InlineData(32)]
    [InlineData(51)]
    [InlineData(0x1000)]
    [InlineData(0x8000)]
    [InlineData(0x8000 - 1)]
    public void CheckRandomBuffer(int size)
    {
        var ldss = new LibDivSufSort();

        using var ownedT = GetOwnedRandomBuffer(size);
        ReadOnlySpan<byte> T = ownedT.Span;
        using var ownedSA = SpanOwner<int>.Allocate(size, AllocationMode.Clear);
        var SA = ownedSA.Span;
        ldss.Sort(T, SA);
        Verify(T, SA);
    }
}
