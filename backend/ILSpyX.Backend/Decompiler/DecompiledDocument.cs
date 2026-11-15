// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Collections.Generic;
using System.Linq;

namespace ILSpyX.Backend.Decompiler;

/// <summary>
/// Target of a reference in decompiled code.
/// </summary>
public abstract record ReferenceTarget;

/// <summary>
/// Type or member definition, identified by its metadata token in the assembly defining it.
/// </summary>
public record SymbolReferenceTarget(AssemblyFileIdentifier Assembly, int Token) : ReferenceTarget;

/// <summary>
/// Metadata reference (e.g. TypeRef or MemberRef in IL code), which still has to be resolved to its definition.
/// </summary>
public record HandleReferenceTarget(AssemblyFileIdentifier Assembly, int Token) : ReferenceTarget;

/// <summary>
/// Local symbol of a document (variable, parameter, label), identified by a number unique within this document.
/// </summary>
public record LocalReferenceTarget(int Id) : ReferenceTarget;

/// <summary>
/// Reference to a symbol in decompiled code. Line and column are zero-based, like positions in LSP.
/// </summary>
public record CodeReference(int Line, int Column, int Length, ReferenceTarget Target, bool IsDefinition)
{
    // The end is included, so a cursor placed directly after an identifier still refers to it.
    public bool Contains(int line, int column) =>
        line == Line && column >= Column && column <= Column + Length;
}

/// <summary>
/// Decompiled code together with the symbol references written to it.
/// </summary>
public record DecompiledDocument(string Code, IReadOnlyList<CodeReference> References)
{
    public CodeReference? FindReferenceAt(int line, int column) =>
        References
            .Where(reference => reference.Contains(line, column))
            // Prefer a reference starting at the position over one ending there.
            .OrderBy(reference => reference.Column + reference.Length == column ? 1 : 0)
            .FirstOrDefault();

    public CodeReference? FindDefinition(ReferenceTarget target) =>
        References.FirstOrDefault(reference => reference.IsDefinition && reference.Target.Equals(target));
}
