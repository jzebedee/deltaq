using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using DeltaQ.SuffixSorting;
using ICSharpCode.SharpZipLib.BZip2;
using System;
using System.Buffers;
using System.IO;

namespace DeltaQ.BsDiff;
using static Constants;

public static class Diff
{
    internal static Stream GetEncodingStream(Stream stream, bool output)
        => output
            ? new BZip2OutputStream(stream)
            : new BZip2InputStream(stream);

    /// <summary>
    /// Creates a BSDIFF-format patch from two byte buffers
    /// </summary>
    /// <param name="oldData">Byte buffer of the original (older) data</param>
    /// <param name="newData">Byte buffer of the changed (newer) data</param>
    /// <param name="output">Seekable, writable stream where the patch will be written</param>
    /// <param name="suffixSort">Suffix sort implementation to use for comparison</param>
    public static void Create(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, Stream output, ISuffixSort suffixSort)
    {
        // check arguments
        if (oldData == null)
            throw new ArgumentNullException(nameof(oldData));
        if (newData == null)
            throw new ArgumentNullException(nameof(newData));
        if (output == null)
            throw new ArgumentNullException(nameof(output));
        if (suffixSort == null)
            throw new ArgumentNullException(nameof(suffixSort));
        if (!output.CanSeek)
            throw new ArgumentException("Output stream must be seekable.", nameof(output));
        if (!output.CanWrite)
            throw new ArgumentException("Output stream must be writable.", nameof(output));

        /* Header is
            0	8	 "BSDIFF40"
            8	8	length of bzip2ed ctrl block
            16	8	length of bzip2ed diff block
            24	8	length of new file
           File is
            0	32	Header
            32	??	Bzip2ed ctrl block
            ??	??	Bzip2ed diff block
            ??	??	Bzip2ed extra block */
        Span<byte> header = stackalloc byte[HeaderSize];
        header[HeaderOffsetSig..].WritePackedLong(Signature);

        header[HeaderOffsetNewData..].WritePackedLong(newData.Length);

        var startPosition = output.Position;
        output.Write(header);

        //backing for ctrl writes
        Span<byte> buf = stackalloc byte[sizeof(long)];

        //the memory allocated for the suffix array MUST be at least (n+1)
        //this is only required for bsdiff, so we allocate it ourselves
        //instead of using the ISuffixSort overloads that only require allocations of (n)
        using var saOwner = MemoryOwner<int>.Allocate(oldData.Length + 1, AllocationMode.Clear);

        using var ctrlSink = new ArrayPoolBufferWriter<byte>();
        using var diffSink = new ArrayPoolBufferWriter<byte>();
        using var extraSink = new ArrayPoolBufferWriter<byte>();

        {
            using var ctrlEncStream = GetEncodingStream(ctrlSink.AsStream(), true);
            using var diffEncStream = GetEncodingStream(diffSink.AsStream(), true);
            using var extraEncStream = GetEncodingStream(extraSink.AsStream(), true);

            Span<int> I = saOwner.Span;
            suffixSort.Sort(oldData, I[..^1]);

            var scan = 0;
            var pos = 0;
            var len = 0;
            var lastscan = 0;
            var lastpos = 0;
            var lastoffset = 0;

            // compute the differences, writing ctrl as we go
            while (scan < newData.Length)
            {
                var oldscore = 0;

                for (var scsc = scan += len; scan < newData.Length; scan++)
                {
                    len = Search(I, oldData, newData[scan..], 0, oldData.Length, out pos);

                    for (; scsc < scan + len; scsc++)
                    {
                        if ((scsc + lastoffset < oldData.Length) && (oldData[scsc + lastoffset] == newData[scsc]))
                            oldscore++;
                    }

                    if ((len == oldscore && len != 0) || (len > oldscore + 8))
                        break;

                    if ((scan + lastoffset < oldData.Length) && (oldData[scan + lastoffset] == newData[scan]))
                        oldscore--;
                }

                if (len != oldscore || scan == newData.Length)
                {
                    var s = 0;
                    var sf = 0;
                    var lenf = 0;
                    for (var i = 0; (lastscan + i < scan) && (lastpos + i < oldData.Length);)
                    {
                        if (oldData[lastpos + i] == newData[lastscan + i])
                            s++;
                        i++;
                        if (s * 2 - i > sf * 2 - lenf)
                        {
                            sf = s;
                            lenf = i;
                        }
                    }

                    var lenb = 0;
                    if (scan < newData.Length)
                    {
                        s = 0;
                        var sb = 0;
                        for (var i = 1; (scan >= lastscan + i) && (pos >= i); i++)
                        {
                            if (oldData[pos - i] == newData[scan - i])
                                s++;
                            if (s * 2 - i > sb * 2 - lenb)
                            {
                                sb = s;
                                lenb = i;
                            }
                        }
                    }

                    if (lastscan + lenf > scan - lenb)
                    {
                        var overlap = (lastscan + lenf) - (scan - lenb);
                        s = 0;
                        var ss = 0;
                        var lens = 0;
                        for (var i = 0; i < overlap; i++)
                        {
                            if (newData[lastscan + lenf - overlap + i] == oldData[lastpos + lenf - overlap + i])
                                s++;
                            if (newData[scan - lenb + i] == oldData[pos - lenb + i])
                                s--;
                            if (s > ss)
                            {
                                ss = s;
                                lens = i + 1;
                            }
                        }

                        lenf += lens - overlap;
                        lenb -= lens;
                    }

                    //write diff string
                    for (var i = 0; i < lenf; i++)
                        diffEncStream.WriteByte((byte)(newData[lastscan + i] - oldData[lastpos + i]));

                    //write extra string
                    var extraLength = (scan - lenb) - (lastscan + lenf);
                    if (extraLength > 0)
                        extraEncStream.Write(newData.Slice(lastscan + lenf, extraLength));

                    //write ctrl block
                    buf.WritePackedLong(lenf);
                    ctrlEncStream.Write(buf);

                    buf.WritePackedLong(extraLength);
                    ctrlEncStream.Write(buf);

                    buf.WritePackedLong((pos - lenb) - (lastpos + lenf));
                    ctrlEncStream.Write(buf);

                    lastscan = scan - lenb;
                    lastpos = pos - lenb;
                    lastoffset = pos - scan;
                }
            }
        }

        //write compressed ctrl data
        output.Write(ctrlSink.WrittenSpan);
        header[HeaderOffsetCtrl..].WritePackedLong(ctrlSink.WrittenCount);

        // write compressed diff data
        output.Write(diffSink.WrittenSpan);
        header[HeaderOffsetDiff..].WritePackedLong(diffSink.WrittenCount);

        // write compressed extra data
        output.Write(extraSink.WrittenSpan);

        // seek to the beginning, write the header, then seek back to end
        var endPosition = output.Position;
        output.Position = startPosition;
        output.Write(header);
        output.Position = endPosition;
    }

    private static int CompareBytes(ReadOnlySpan<byte> left, ReadOnlySpan<byte> right)
        => left.SequenceCompareTo(right);

    private static int MatchLength(ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData)
    {
        int i;
        for (i = 0; i < oldData.Length && i < newData.Length; i++)
        {
            if (oldData[i] != newData[i])
                break;
        }

        return i;
    }

    private static int Search(ReadOnlySpan<int> I, ReadOnlySpan<byte> oldData, ReadOnlySpan<byte> newData, int start, int end, out int pos)
    {
        while (true)
        {
            if (end - start < 2)
            {
                var x = MatchLength(oldData[I[start]..], newData);
                var y = MatchLength(oldData[I[end]..], newData);

                if (x > y)
                {
                    pos = I[start];
                    return x;
                }
                else
                {
                    pos = I[end];
                    return y;
                }
            }

            var midPoint = start + (end - start) / 2;
            if (CompareBytes(oldData[I[midPoint]..], newData) < 0)
            {
                start = midPoint;
            }
            else
            {
                end = midPoint;
            }
        }
    }
}
