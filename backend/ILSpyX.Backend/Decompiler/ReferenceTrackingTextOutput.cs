// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Disassembler;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using System.Collections.Generic;
using System.IO;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Text;

namespace ILSpyX.Backend.Decompiler;

/// <summary>
/// Plain text output, which additionally records the positions and targets of all symbol references
/// written by the decompiler or disassembler.
/// </summary>
public sealed class ReferenceTrackingTextOutput : ITextOutput
{
    private readonly PositionTrackingWriter writer = new();
    private readonly PlainTextOutput output;
    private readonly List<CodeReference> references = [];
    private readonly Dictionary<MetadataFile, AssemblyFileIdentifier?> assemblyIdentifiers = [];
    // Local symbols are replaced by IDs, so documents don't keep the decompiler's objects alive.
    private readonly Dictionary<object, int> localIds = [];

    public ReferenceTrackingTextOutput()
    {
        output = new PlainTextOutput(writer);
    }

    public DecompiledDocument ToDocument() => new(writer.ToString(), references);

    public override string ToString() => writer.ToString();

    public string IndentationString
    {
        get => output.IndentationString;
        set => output.IndentationString = value;
    }

    public void Indent() => output.Indent();

    public void Unindent() => output.Unindent();

    public void Write(char ch) => output.Write(ch);

    public void Write(string text) => output.Write(text);

    public void WriteLine() => output.WriteLine();

    public void WriteReference(OpCodeInfo opCode, bool omitSuffix = false) =>
        output.WriteReference(opCode, omitSuffix);

    public void WriteReference(MetadataFile metadata, Handle handle, string text, string protocol = "decompile",
        bool isDefinition = false)
    {
        output.WriteReference(metadata, handle, text, protocol, isDefinition);
        if (protocol == "decompile" && !handle.IsNil && GetAssemblyIdentifier(metadata) is { } assembly)
        {
            ReferenceTarget? target = handle.Kind switch
            {
                HandleKind.TypeDefinition or HandleKind.MethodDefinition or HandleKind.FieldDefinition
                    or HandleKind.PropertyDefinition or HandleKind.EventDefinition =>
                    new SymbolReferenceTarget(assembly, MetadataTokens.GetToken(handle)),
                HandleKind.TypeReference or HandleKind.MemberReference or HandleKind.MethodSpecification
                    or HandleKind.TypeSpecification =>
                    new HandleReferenceTarget(assembly, MetadataTokens.GetToken(handle)),
                _ => null
            };
            AddReference(text, target, isDefinition);
        }
    }

    public void WriteReference(IType type, string text, bool isDefinition = false)
    {
        output.WriteReference(type, text, isDefinition);
        AddReference(text, GetSymbolTarget(type.GetDefinition()), isDefinition);
    }

    public void WriteReference(IMember member, string text, bool isDefinition = false)
    {
        output.WriteReference(member, text, isDefinition);
        AddReference(text, GetSymbolTarget(member.MemberDefinition), isDefinition);
    }

    public void WriteLocalReference(string text, object reference, bool isDefinition = false,
        bool isHoverOnly = false)
    {
        output.WriteLocalReference(text, reference, isDefinition, isHoverOnly);
        if (reference is not null)
        {
            if (!localIds.TryGetValue(reference, out int id))
            {
                id = localIds.Count;
                localIds[reference] = id;
            }

            AddReference(text, new LocalReferenceTarget(id), isDefinition);
        }
    }

    public void MarkFoldStart(string collapsedText = "...", bool defaultCollapsed = false,
        bool isDefinition = false) =>
        ((ITextOutput) output).MarkFoldStart(collapsedText, defaultCollapsed, isDefinition);

    public void MarkDefinitionStart() => ((ITextOutput) output).MarkDefinitionStart();

    public void MarkFoldEnd() => ((ITextOutput) output).MarkFoldEnd();

    private SymbolReferenceTarget? GetSymbolTarget(IEntity? entity)
    {
        if (entity?.ParentModule?.MetadataFile is not { } metadataFile || entity.MetadataToken.IsNil)
        {
            return null;
        }

        return GetAssemblyIdentifier(metadataFile) is { } assembly
            ? new SymbolReferenceTarget(assembly, MetadataTokens.GetToken(entity.MetadataToken))
            : null;
    }

    private AssemblyFileIdentifier? GetAssemblyIdentifier(MetadataFile metadataFile)
    {
        if (!assemblyIdentifiers.TryGetValue(metadataFile, out var assembly))
        {
            assembly = metadataFile.GetAssemblyFileIdentifier();
            assemblyIdentifiers[metadataFile] = assembly;
        }

        return assembly;
    }

    private void AddReference(string text, ReferenceTarget? target, bool isDefinition)
    {
        // The C# output also writes punctuation (parentheses, semicolons, operators) as references,
        // only identifiers are relevant for navigation.
        // The reference text has just been written, so it ends at the current position.
        if (target is not null && IsIdentifier(text) && writer.Column >= text.Length)
        {
            references.Add(new CodeReference(writer.Line, writer.Column - text.Length, text.Length, target,
                isDefinition));
        }
    }

    private static bool IsIdentifier(string text)
    {
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '_')
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// String writer keeping track of the current line and column.
    /// </summary>
    private sealed class PositionTrackingWriter : StringWriter
    {
        private int lineStart;

        public int Line { get; private set; }
        public int Column => GetStringBuilder().Length - lineStart;

        public override void Write(char value)
        {
            base.Write(value);
            if (value == '\n')
            {
                Line++;
                lineStart = GetStringBuilder().Length;
            }
        }

        public override void Write(string? value)
        {
            if (value is null)
            {
                return;
            }

            base.Write(value);
            UpdatePosition(value);
        }

        public override void Write(char[] buffer, int index, int count)
        {
            base.Write(buffer, index, count);
            UpdatePosition(new string(buffer, index, count));
        }

        private void UpdatePosition(string value)
        {
            int lastNewLine = value.LastIndexOf('\n');
            if (lastNewLine < 0)
            {
                return;
            }

            foreach (char c in value)
            {
                if (c == '\n')
                {
                    Line++;
                }
            }

            StringBuilder builder = GetStringBuilder();
            lineStart = builder.Length - (value.Length - lastNewLine - 1);
        }
    }
}
