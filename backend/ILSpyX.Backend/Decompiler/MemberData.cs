// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler.TypeSystem;

namespace ILSpyX.Backend.Decompiler;

public record MemberData(string? Name, int Token, TypeKind SubKind);
