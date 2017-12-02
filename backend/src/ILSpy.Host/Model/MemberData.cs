// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using Mono.Cecil;

namespace ILSpy.Host
{
    public class MemberData
    {
        public string Name { get; set; }

        public MetadataToken Token { get; set; }

        public MemberSubKind MemberSubKind { get; set; }
    }
}
