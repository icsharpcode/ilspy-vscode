// Copyright (c) .NET Foundation and Contributors. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace ILSpy.Host
{
    internal class ListNamespacesResponse
    {
        public IEnumerable<string> Namespaces { get; set; }
    }
}