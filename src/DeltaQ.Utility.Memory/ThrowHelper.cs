//Generated 2021-12-26
//https://raw.githubusercontent.com/dotnet/runtime/84680bf557210114ea5ca823386cd49691c4cac6/src/libraries/System.Private.CoreLib/src/System/ThrowHelper.cs

#if !(NETCOREAPP3_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER)

// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace System;

internal static class ThrowHelper
{
    [DoesNotReturn]
    internal static void ThrowValueArgumentOutOfRange_NeedNonNegNumException()
    {
        throw GetArgumentOutOfRangeException(ExceptionArgument.value, ExceptionResource.ArgumentOutOfRange_NeedNonNegNum);
    }

    [DoesNotReturn]
    internal static void ThrowArgumentOutOfRangeException(ExceptionArgument argument)
    {
        throw new ArgumentOutOfRangeException(GetArgumentName(argument));
    }

    private static ArgumentOutOfRangeException GetArgumentOutOfRangeException(ExceptionArgument argument, ExceptionResource resource)
    {
        return new ArgumentOutOfRangeException(GetArgumentName(argument), GetResourceString(resource));
    }

    private static string GetArgumentName(ExceptionArgument argument)
    {
        switch (argument)
        {
            case ExceptionArgument.value:
                return "value";
            case ExceptionArgument.length:
                return "length";
            default:
                Debug.Fail("The enum value is not defined, please check the ExceptionArgument Enum.");
                return "";
        }
    }

    private static string GetResourceString(ExceptionResource resource)
    {
        switch (resource)
        {
            case ExceptionResource.ArgumentOutOfRange_NeedNonNegNum:
                return "Non-negative number required.";
            default:
                Debug.Fail("The enum value is not defined, please check the ExceptionResource Enum.");
                return "";
        }
    }
}

internal enum ExceptionArgument
{
    value,
    length
}

internal enum ExceptionResource
{
    ArgumentOutOfRange_NeedNonNegNum,
}
#endif