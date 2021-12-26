//Generated 2021-12-26
//https://raw.githubusercontent.com/dotnet/runtime/84680bf557210114ea5ca823386cd49691c4cac6/src/libraries/System.Private.CoreLib/src/System/Numerics/Hashing/HashHelpers.cs

#if NETSTANDARD2_0
// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

namespace System.Numerics.Hashing
{
    internal static class HashHelpers
    {
        public static int Combine(int h1, int h2)
        {
            // RyuJIT optimizes this to use the ROL instruction
            // Related GitHub pull request: https://github.com/dotnet/coreclr/pull/1830
            uint rol5 = ((uint)h1 << 5) | ((uint)h1 >> 27);
            return ((int)rol5 + h1) ^ h2;
        }
    }
}
#endif