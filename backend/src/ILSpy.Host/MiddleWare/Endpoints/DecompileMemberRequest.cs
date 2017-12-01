﻿// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Host
{
    public class DecompileMemberRequest : RequestBase
    {
        public uint TypeRid { get; set; }
        public uint MemberType { get; set; }
        public uint MemberRid { get; set; }
    }
}
