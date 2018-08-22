// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;

namespace ILSpy.Host
{
    public class DecompileCode
    {
        /// <summary>
        /// Gets or sets the decompiled code, each key-value pair contains
        ///    key: programming language, "CSharp" or "IL"
        ///    value: decompiled code
        /// </summary>
        public IDictionary<string, string> Decompiled { get; set; }
    }
}
