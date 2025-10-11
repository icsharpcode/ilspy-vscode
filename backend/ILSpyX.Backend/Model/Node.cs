// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpyX.Backend.Model;

public record Node()
{
    public required NodeMetadata? Metadata { get; init; }
    public required string DisplayName { get; init; }
    public required string Description { get; init; }
    public bool MayHaveChildren { get; init; }
    public SymbolModifiers SymbolModifiers { get; init; } = SymbolModifiers.None;
    public NodeFlags Flags { get; init; } = NodeFlags.None;
}