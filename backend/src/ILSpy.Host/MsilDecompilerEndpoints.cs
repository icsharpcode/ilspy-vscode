// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Host
{
    public static class MsilDecompilerEndpoints
    {
        public const string AddAssembly = "/addassembly";
        public const string RemoveAssembly = "/removeassembly";
        public const string DecompileAssembly = "/decompileassembly";
        public const string ListNamespaces = "/listnamespaces";
        public const string ListTypes = "/listtypes";
        public const string DecompileType = "/decompiletype";
        public const string ListMembers = "/listmembers";
        public const string DecompileMember = "/decompilemember";
        public const string StopServer = "/stopserver";
    }
}
