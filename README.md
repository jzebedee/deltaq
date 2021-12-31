# ![DeltaQ logo](assets/dq.png) DeltaQ

Fast and portable delta encoding for .NET in 100% safe, managed code.

DeltaQ is available for use as a library in .NET and .NET Framework, and as a cross-platform command-line tool, `dq`, which can be used to perform delta operations (similar to `bsdiff` or `xdelta`).

## Support

Discussion and technical support is available on Discord.

[![Discord](https://img.shields.io/discord/359127425558249482)](https://discord.gg/FkRPyz6kcD)

## Installing

### `dq` command-line tool

[![DeltaQ.CommandLine nuget package](https://img.shields.io/nuget/v/DeltaQ.CommandLine.svg?style=flat)](https://www.nuget.org/packages/DeltaQ.CommandLine)

`> dotnet tool install DeltaQ.CommandLine -g`

### `DeltaQ` library

[![DeltaQ nuget package](https://img.shields.io/nuget/v/DeltaQ.svg?style=flat)](https://www.nuget.org/packages/DeltaQ)

`> dotnet add package DeltaQ`

## Usage

### `dq` command-line tool

#### Create a binary delta (diff) with BsDiff

`dq bsdiff <oldfile> <newfile> <deltafile>`

Here's an example of `dq` creating a `bsdiff` delta for patching file `app_v1.exe` into `app_v2.exe`:
```
> ls -sh
total 32M
16M app_v1.exe  17M app_v2.exe

> dq bsdiff app_v1.exe app_v2.exe v1_to_v2.delta
Generating BsDiff delta between
Old file: "app_v1.exe"
New file: "app_v2.exe"

Delta file: "v1_to_v2.delta"
Delta size: 4.28 MB (13.49%)
```

#### Apply a binary delta (patch) with BsDiff

`dq bspatch <oldfile> <deltafile> <newfile>`

Instead of distributing the large `app_v2.exe` when it's time to upgrade, `dq` can recreate it by applying the much smaller delta file `v1_to_v2.delta` to the original `app_v1.exe`:

```
> dq bspatch app_v1.exe v1_to_v2.delta generated_app_v2.exe
Applying BsDiff delta between
Old file:   "app_v1.exe"
Delta file: "v1_to_v2.delta"

New file: "generated_app_v2.exe"
```
```
> sha256sum app_v2.exe generated_app_v2.exe
fab165a6e604dc7f9265d13013b6fb06319faec4eaa251a8a6d74a7e30e38dc6  app_v2.exe
fab165a6e604dc7f9265d13013b6fb06319faec4eaa251a8a6d74a7e30e38dc6  generated_app_v2.exe
```

### `DeltaQ` library

The `DeltaQ` package contains all currently supported delta encoding and suffix sorting providers, for use in your own .NET projects.

#### Example: bsdiff and bspatch files

```cs
using System.IO;
using DeltaQ.BsDiff;
using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;

void MakeDelta() {
    var oldData = File.ReadAllBytes("oldfile.txt");
    var newData = File.ReadAllBytes("newfile.txt");
    using var outStream = File.Create("old_to_new.delta");
    ISuffixSort suffixSorter = new LibDivSufSort();

    Diff.Create(oldData, newData, outStream, suffixSorter);
}

void UseDelta() {
    var oldData = File.ReadAllBytes("oldfile.txt");
    var deltaData = File.ReadAllBytes("old_to_new.delta");
    using var outStream = File.Create("generated_newfile.txt");

    Patch.Apply(oldData, deltaData, outStream);
}
```

#### Example: Suffix sorting with LibDivSufSort

```cs
using DeltaQ.SuffixSorting;
using DeltaQ.SuffixSorting.LibDivSufSort;

ISuffixSort suffixSorter = new LibDivSufSort();

ReadOnlySpan<byte> text = new byte[] { 1, 2, 3, 4 };
using var ownedSuffixArray = suffixSorter.Sort(text);
ReadOnlySpan<int> sortedSuffixes = ownedSuffixArray.Memory.Span;
```

