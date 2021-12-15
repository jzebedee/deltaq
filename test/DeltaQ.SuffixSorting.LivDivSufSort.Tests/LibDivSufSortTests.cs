using DeltaQ.SuffixSorting.LibDivSufSort;
using Microsoft.Toolkit.HighPerformance.Buffers;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using Xunit;

namespace DeltaQ.Tests
{
    public class LibDivSufSortTests
    {
        private const string FuzzFilesBasePath = "assets/";

        [Conditional("DEBUG")]
        private void SetupCrosscheckListeners()
        {
            const string crosscheckFilename = "crosscheck/csharp";
            try
            {
                Directory.CreateDirectory(crosscheckFilename);
                File.Create(crosscheckFilename).Dispose();
            }
            catch (IOException) { }
            //var dtl = Trace.Listeners[0] as DefaultTraceListener;
            //dtl!.LogFileName = "crosscheck/csharp";
            var lflistener = new TextWriterTraceListener(crosscheckFilename);
            Trace.Listeners.Clear();
            Trace.Listeners.Add(lflistener);
        }

        [Conditional("DEBUG")]
        private void FinalizeCrosscheck()
        {
            Trace.Flush();
        }

#if NET461
        private static void RandomFillBuffer(byte[] buffer)
        {
            var rand = new Random(63 * 13 * 63 * 13);
            rand.NextBytes(buffer);
        }
#else
        private static SpanOwner<byte> GetOwnedRandomBuffer(int size)
        {
            var rand = new Random(63 * 13 * 63 * 13);

            var owner = SpanOwner<byte>.Allocate(size);
            rand.NextBytes(owner.Span);

            return owner;
        }
#endif

        private static void Verify(ReadOnlySpan<byte> input, ReadOnlySpan<int> sa)
        {
            //ref byte suff(int index) => ref input[sa[index]];
            for (int i = 0; i < input.Length - 1; i++)
            {
                //if(!(suff(i) < suff(i + 1)))
                var cur = input[sa[i]..];
                var next = input[sa[i + 1]..];
                var cmp = cur.SequenceCompareTo(next);
                if (!(cmp < 0))
                //if (!(cur < next))
                {
                    var ex = new InvalidOperationException("Input was unsorted");
                    ex.Data["i"] = i;
                    ex.Data["j"] = i + 1;
                    throw ex;
                }
            }
        }

        [Fact]
        public void CheckShruggy()
        {
            const string shruggy = @"¯\_(ツ)_/¯";

            ReadOnlySpan<byte> T = Encoding.UTF8.GetBytes(shruggy);

            using var ownedSA = SpanOwner<int>.Allocate(T.Length, AllocationMode.Clear);
            var SA = ownedSA.Span;

            DivSufSort.divsufsort(T, SA);
            Verify(T, SA);
        }

        public static IEnumerable<object[]> FuzzFiles => FuzzFilesInner.Select(fuzzFile => new object[] { Path.Join(FuzzFilesBasePath, fuzzFile) });
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
                //yield return prefix + "";
            }
        }

        [Theory]
        [MemberData(nameof(FuzzFiles))]
        public void CheckFile(string path)
        {
            SetupCrosscheckListeners();

            var bytes = File.ReadAllBytes(path);
            ReadOnlySpan<byte> T = bytes;

            using var ownedSA = SpanOwner<int>.Allocate(T.Length, AllocationMode.Clear);
            var SA = ownedSA.Span;

            DivSufSort.divsufsort(T, SA);
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
        //[InlineData(0x80000)]
        //[InlineData(0x800000)]
        public void CheckRandomBuffer(int size)
        {
#if NET461
            var ownedT = ArrayPool<byte>.Shared.Rent(size);
            try
#else
            using (var ownedT = GetOwnedRandomBuffer(size))
#endif
            {
#if NET461
                RandomFillBuffer(ownedT);
                ReadOnlySpan<byte> T = ownedT;
#else
                ReadOnlySpan<byte> T = ownedT.Span;
#endif
                using (var ownedSA = SpanOwner<int>.Allocate(size, AllocationMode.Clear))
                {
                    var SA = ownedSA.Span;

                    DivSufSort.divsufsort(T, SA);
                    Verify(T, SA);
                }
            }
#if NET461
            finally
            {
                ArrayPool<byte>.Shared.Return(ownedT);
            }
#endif
        }
    }
}
