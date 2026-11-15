// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpyX.Backend.Model;

/// <summary>
/// Location of a symbol in decompiled code, e.g. the target of "Go to Definition".
/// Line and column are zero-based, like positions in LSP.
/// </summary>
/// <param name="Node">Node whose decompiled code contains the location, null for the requesting document itself.</param>
public record CodeLocation(Node? Node, int Line, int Column, int Length);
