/*
    Adapted from https://github.com/dotnet/runtime/blob/master/src/libraries/Common/src/System/SR.cs
    and https://source.dot.net/System.Collections/System.SR.cs.html#b31ecb5e5b3556b0

    Licensed to the .NET Foundation under one or more agreements.
    The .NET Foundation licenses this file to you under the MIT license.
    See LICENSE.TXT at https://github.com/dotnet/runtime/blob/master/LICENSE.TXT
*/

#nullable enable

using System;
using System.Resources;

namespace VideoScriptEditor.Core
{
    internal class SystemCollectionsSR
    {
        private static ResourceManager? s_resourceManager;
        internal static ResourceManager ResourceManager => s_resourceManager ??= new ResourceManager("FxResources.System.Collections.SR", typeof(System.Collections.Generic.SortedList<,>).Assembly);

        /// <summary>The lower bound of target array must be zero.</summary>
        internal static string @Arg_NonZeroLowerBound => GetResourceString("Arg_NonZeroLowerBound", @"The lower bound of target array must be zero.");
        /// <summary>The value '{0}' is not of type '{1}' and cannot be used in this generic collection.</summary>
        internal static string @Arg_WrongType => GetResourceString("Arg_WrongType", @"The value '{0}' is not of type '{1}' and cannot be used in this generic collection.");
        /// <summary>Destination array is not long enough to copy all the items in the collection. Check array index and length.</summary>
        internal static string @Arg_ArrayPlusOffTooSmall => GetResourceString("Arg_ArrayPlusOffTooSmall", @"Destination array is not long enough to copy all the items in the collection. Check array index and length.");
        /// <summary>Non-negative number required.</summary>
        internal static string @ArgumentOutOfRange_NeedNonNegNum => GetResourceString("ArgumentOutOfRange_NeedNonNegNum", @"Non-negative number required.");
        /// <summary>capacity was less than the current size.</summary>
        internal static string @ArgumentOutOfRange_SmallCapacity => GetResourceString("ArgumentOutOfRange_SmallCapacity", @"capacity was less than the current size.");
        /// <summary>An item with the same key has already been added. Key: {0}</summary>
        internal static string @Argument_AddingDuplicate => GetResourceString("Argument_AddingDuplicate", @"An item with the same key has already been added. Key: {0}");
        /// <summary>Enumeration has either not started or has already finished.</summary>
        internal static string @InvalidOperation_EnumOpCantHappen => GetResourceString("InvalidOperation_EnumOpCantHappen", @"Enumeration has either not started or has already finished.");
        /// <summary>Collection was modified after the enumerator was instantiated.</summary>
        internal static string @InvalidOperation_EnumFailedVersion => GetResourceString("InvalidOperation_EnumFailedVersion", @"Collection was modified after the enumerator was instantiated.");
        /// <summary>Mutating a key collection derived from a dictionary is not allowed.</summary>
        internal static string @NotSupported_KeyCollectionSet => GetResourceString("NotSupported_KeyCollectionSet", @"Mutating a key collection derived from a dictionary is not allowed.");
        /// <summary>Only single dimensional arrays are supported for the requested action.</summary>
        internal static string @Arg_RankMultiDimNotSupported => GetResourceString("Arg_RankMultiDimNotSupported", @"Only single dimensional arrays are supported for the requested action.");
        /// <summary>Target array type is not compatible with the type of items in the collection.</summary>
        internal static string @Argument_InvalidArrayType => GetResourceString("Argument_InvalidArrayType", @"Target array type is not compatible with the type of items in the collection.");
        /// <summary>Index was out of range. Must be non-negative and less than the size of the collection.</summary>
        internal static string @ArgumentOutOfRange_Index => GetResourceString("ArgumentOutOfRange_Index", @"Index was out of range. Must be non-negative and less than the size of the collection.");
        /// <summary>This operation is not supported on SortedList nested types because they require modifying the original SortedList.</summary>
        internal static string @NotSupported_SortedListNestedWrite => GetResourceString("NotSupported_SortedListNestedWrite", @"This operation is not supported on SortedList nested types because they require modifying the original SortedList.");
        /// <summary>The given key '{0}' was not present in the dictionary.</summary>
        internal static string @Arg_KeyNotFoundWithKey => GetResourceString("Arg_KeyNotFoundWithKey", @"The given key '{0}' was not present in the dictionary.");

#if (!NETSTANDARD1_0 && !NETSTANDARD1_1 && !NET45) // AppContext is not supported on < NetStandard1.3 or < .NET Framework 4.5
        private static readonly bool s_usingResourceKeys = AppContext.TryGetSwitch("System.Resources.UseSystemResourceKeys", out bool usingResourceKeys) ? usingResourceKeys : false;
#endif

        // This method is used to decide if we need to append the exception message parameters to the message when calling SR.Format.
        // by default it returns the value of System.Resources.UseSystemResourceKeys AppContext switch or false if not specified.
        // Native code generators can replace the value this returns based on user input at the time of native code generation.
        // The Linker is also capable of replacing the value of this method when the application is being trimmed.
        private static bool UsingResourceKeys() =>
#if (!NETSTANDARD1_0 && !NETSTANDARD1_1 && !NET45) // AppContext is not supported on < NetStandard1.3 or < .NET Framework 4.5
            s_usingResourceKeys;
#else
            false;
#endif

        internal static string GetResourceString(string resourceKey, string? defaultString = null)
        {
            if (UsingResourceKeys())
            {
                return defaultString ?? resourceKey;
            }

            string? resourceString = null;
            try
            {
                resourceString = ResourceManager.GetString(resourceKey);
            }
            catch (MissingManifestResourceException) { }

            if (defaultString != null && resourceKey.Equals(resourceString))
            {
                return defaultString;
            }

            return resourceString!; // only null if missing resources
        }

        internal static string Format(string resourceFormat, object? p1)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1);
            }

            return string.Format(resourceFormat, p1);
        }

        internal static string Format(string resourceFormat, object? p1, object? p2)
        {
            if (UsingResourceKeys())
            {
                return string.Join(", ", resourceFormat, p1, p2);
            }

            return string.Format(resourceFormat, p1, p2);
        }
    }
}
