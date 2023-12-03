using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using DeltaQ.BsDiff;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using Xunit;

namespace DeltaQ.Tests;

public class BsDiffTests
{
    private static readonly int[] Sizes = { 0, 1, 512, 999, 1024, 0x10000 };

    private static byte[] GetBuffer(int size)
    {
        var rand = new Random(63 * 13 * 63 * 13);

        var buf = new byte[size];
        rand.NextBytes(buf);

        return buf;
    }

    public static IEnumerable<object[]> TestDoubleBuffers(IEnumerable<int> sizes)
        => sizes.Select(size => new object[] { GetBuffer(size), GetBuffer(size) });

    [Theory]
    [MemberData(nameof(TestDoubleBuffers), [new int[] { 0, 1, 512, 999, 1024, 4096 }])]
    public void BsDiffRoundtripFromBuffers(byte[] oldBuffer, byte[] newBuffer)
    {
        var patchBuf = BsDiffCreate(oldBuffer, newBuffer);
        var finishedBuf = BsDiffApply(oldBuffer, patchBuf);

        Assert.True(newBuffer.AsSpan().SequenceEqual(finishedBuf.Span));
    }

    public static IEnumerable<object[]> TestSingleBuffers(IEnumerable<int> sizes)
       => sizes.Select(size => new object[] { GetBuffer(size) });

    [Theory]
    [MemberData(nameof(TestSingleBuffers), [new int[] { 0, 1, 512, 999, 1024, 4096 }])]
    public void BsDiffRoundtripFromBuffers_Identical(byte[] oldBuffer)
    {
        var newBuffer = new byte[oldBuffer.Length];
        Buffer.BlockCopy(oldBuffer, 0, newBuffer, 0, oldBuffer.Length);

        var patchBuf = BsDiffCreate(oldBuffer, newBuffer);
        var finishedBuf = BsDiffApply(oldBuffer, patchBuf);

        Assert.True(oldBuffer.AsSpan().SequenceEqual(finishedBuf.Span));
        Assert.True(newBuffer.AsSpan().SequenceEqual(finishedBuf.Span));
    }

    [Theory]
    [MemberData(nameof(TestDoubleBuffers), [new int[] { 0, 1, 512, 999, 1024, 4096 }])]
    public void BsDiffRoundtripFromStreams(byte[] oldData, byte[] newData)
    {
        using var outputOwner = MemoryOwner<byte>.Allocate(0x2000);

        Diff.Create(oldData, newData, outputOwner.Memory.AsStream(), new SuffixSorting.SAIS.SAIS());

        using var msOld = new MemoryStream(oldData);
        using var msPatchOutput = new MemoryStream();
        Patch.Apply(msOld, OpenPatchStream, msPatchOutput);

        Span<byte> newSpan, reconstructedSpan;
        newSpan = newData;
        reconstructedSpan = msPatchOutput.GetBuffer().AsSpan(0, (int)msPatchOutput.Length);

        Assert.True(newSpan.SequenceEqual(reconstructedSpan));
        return;

        Stream OpenPatchStream(long start, long len)
            => (len > 0 ? outputOwner.Memory.Slice((int)start, (int)len) : outputOwner.Memory.Slice((int)start)).AsStream();
    }

    [Theory]
    [MemberData(nameof(BsDiffCreateNullArguments_TestData))]
    public void BsDiffCreateNullArgumentsThrows(byte[] oldData, byte[] newData, Stream outStream)
    {
        Assert.Throws<ArgumentNullException>(() => Diff.Create(oldData, newData, outStream, new SuffixSorting.SAIS.SAIS()));
    }

    public static IEnumerable<object[]> BsDiffCreateNullArguments_TestData()
    {
        var emptybuf = Array.Empty<byte>();
        var ms = new MemoryStream();
        yield return new object[] { null, emptybuf, ms };
        yield return new object[] { emptybuf, null, ms };
        yield return new object[] { emptybuf, emptybuf, null };
    }

    [Theory]
    [MemberData(nameof(BsDiffCreateBadStreams_TestData))]
    public void BsDiffCreateBadStreamsThrows(byte[] oldData, byte[] newData, Stream outStream)
    {
        Assert.Throws<ArgumentException>(() => Diff.Create(oldData, newData, outStream, new SuffixSorting.SAIS.SAIS()));
    }

    public static IEnumerable<object[]> BsDiffCreateBadStreams_TestData()
    {
        var emptybuf = new byte[0];
        yield return new object[] { emptybuf, emptybuf, new MemoryStream(emptybuf, false) };
        yield return new object[] { emptybuf, emptybuf, new DeflateStream(new MemoryStream(), CompressionMode.Compress) };
    }

    private static ReadOnlyMemory<byte> BsDiffCreate(ReadOnlySpan<byte> oldBuf, ReadOnlySpan<byte> newBuf)
    {
        var outputStream = new MemoryStream();
        Diff.Create(oldBuf, newBuf, outputStream, new SuffixSorting.SAIS.SAIS());
        return outputStream.GetBuffer().AsMemory(0, (int)outputStream.Length);
    }

    private static ReadOnlyMemory<byte> BsDiffApply(ReadOnlyMemory<byte> oldBuffer, ReadOnlyMemory<byte> patchBuffer)
    {
        var outputStream = new MemoryStream();
        Patch.Apply(oldBuffer, patchBuffer, outputStream);
        return outputStream.GetBuffer().AsMemory(0, (int)outputStream.Length);
    }
}
