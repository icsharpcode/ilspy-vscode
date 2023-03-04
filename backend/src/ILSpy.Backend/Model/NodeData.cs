// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpy.Backend.Model;

public record NodeData(
    Node? Node,
    string DisplayName,
    string Description,
    bool MayHaveChildren,
    SymbolModifiers SymbolModifiers);
