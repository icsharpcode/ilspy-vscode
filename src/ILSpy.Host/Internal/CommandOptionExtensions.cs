// See the LICENSE file in the project root for more information.

using System.ComponentModel;
using Microsoft.Extensions.CommandLineUtils;

namespace MsilDecompiler.Host.Internal
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
