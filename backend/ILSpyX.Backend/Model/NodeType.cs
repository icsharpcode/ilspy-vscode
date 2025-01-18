// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpyX.Backend.Model;

public enum NodeType
{
    Unknown,
    Assembly,
    Namespace,
    Class,
    Interface,
    Struct,
    Enum,
    Delegate,
    Event,
    Field,
    Method,
    Const,
    Property,
    AssemblyReference,
    ReferencesRoot,
    Analyzer
}
