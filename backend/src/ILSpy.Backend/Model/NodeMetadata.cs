// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpy.Backend.Model;

public record NodeMetadata(string AssemblyPath, NodeType Type, string Name, int SymbolToken, int ParentSymbolToken);
