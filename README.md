#deltaq

Fast and portable delta encoding library for .NET

deltaq is a [.NET Core](https://dotnet.github.io/) class library

## Installing

* Manual install: Download from the [Releases](https://github.com/jzebedee/deltaq/releases/)
* NuGet install: Follow instructions on the [NuGet page](https://www.nuget.org/packages/deltaq/) or enter ```Install-Package deltaq``` in the Package Manager console.

### Supported formats
|Format|Create patches|Apply patches|
|------|--------------|-------------|
|bsdiff|Yes|Yes|
|vcdiff|No|No|

### Roadmap

* Add support for applying VCDIFF patches. VCDIFF format is defined in [RFC 3284](https://tools.ietf.org/html/rfc3284) with several existing implementations. Jon Skeet's [MiscUtil](http://www.yoda.arachsys.com/csharp/miscutil/) already has an implementation of the patch portion of VCDIFF, but much more work is needed to create a C# patch generator.
* Add platform-specific libraries to make usage as simple as possible. There's also room to support memory-mapped files and similar significant optimizations.
