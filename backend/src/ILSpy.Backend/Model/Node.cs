// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpy.Backend.Model;

public record Node(string AssemblyPath, NodeType Type, int SymbolToken, int ParentSymbolToken);
