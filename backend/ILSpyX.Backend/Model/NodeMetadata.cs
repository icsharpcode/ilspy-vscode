// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpyX.Backend.Model;

public record NodeMetadata
{
    public required string AssemblyPath { get; init; }
    public required NodeType Type { get; init; }
    public required string Name { get; init; }
    public string? BundledAssemblyName { get; init; }
    public int SymbolToken { get; init; } // = 0
    public int ParentSymbolToken { get; init; } // = 0
    public bool IsDecompilable { get; init; } // = false
    public string? SubType { get; init; }
}