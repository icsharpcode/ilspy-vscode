// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler;

namespace ILSpy.Host.Configuration
{
    public interface IDecompilationConfiguration
    {
        string FilePath { get; }
        DecompilerSettings DecompilerSettings { get; }
    }
}
