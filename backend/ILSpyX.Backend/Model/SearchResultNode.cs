// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

using System.Diagnostics.CodeAnalysis;

namespace ILSpyX.Backend.Model;

/// <summary>
/// Location of a search match in the decompiled code of a search result node.
/// Line and column are zero-based, like positions in LSP.
/// </summary>
public record SearchMatch(int Line, int Column, int Length, string Text);

public record SearchResultNode : Node
{
    [SetsRequiredMembers]
    public SearchResultNode(Node node) : base(node)
    {
    }

    public SearchMatch? Match { get; init; }
}
