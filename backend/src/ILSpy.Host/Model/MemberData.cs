// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler.TypeSystem;

namespace ILSpy.Host
{
    public class MemberData
    {
        public string Name { get; set; }

        public int Token { get; set; }

        public TypeKind MemberSubKind { get; set; }
    }
}
