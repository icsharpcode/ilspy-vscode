// Copyright (c) ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpyX.Backend.Model;

/// <summary>
/// Zero-based line/character position of a node's declaration within the decompiled code of its parent node.
/// </summary>
public record NodePosition(
    int SymbolToken,
    int StartLine,
    int StartCharacter,
    int EndLine,
    int EndCharacter);
