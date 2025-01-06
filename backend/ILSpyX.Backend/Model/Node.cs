// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpyX.Backend.Model;

public record Node(
    NodeMetadata? Metadata,
    string DisplayName,
    string Description,
    bool MayHaveChildren,
    SymbolModifiers SymbolModifiers);
