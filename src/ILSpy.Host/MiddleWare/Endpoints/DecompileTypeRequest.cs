// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Host
{
    public class DecompileTypeRequest : RequestBase
    {
        public uint Rid { get; set; }
    }
}
