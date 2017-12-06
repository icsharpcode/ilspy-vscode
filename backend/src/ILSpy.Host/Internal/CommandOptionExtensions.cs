// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.Extensions.CommandLineUtils;

namespace ILSpy.Host.Internal
{
    internal static class CommandOptionExtensions
    {
        internal static T GetValueOrDefault<T>(this CommandOption opt, T defaultValue)
        {
            if (opt.HasValue())
            {
                return (T)TypeDescriptor.GetConverter(typeof(T)).ConvertFrom(opt.Value());
            }

            return defaultValue;
        }
    }
}
