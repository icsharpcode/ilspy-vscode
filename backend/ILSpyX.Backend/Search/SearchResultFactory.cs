﻿// Copyright (c) 2022 Siegfried Pammer
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

using System;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Abstractions;
using ICSharpCode.ILSpyX.Search;
using ILSpy.Backend.Decompiler;

namespace ILSpyX.Backend.Search;

internal class SearchResultFactory : ISearchResultFactory
{
    private readonly ILanguage language = new CSharpLanguage();

    public SearchResultFactory()
    {
    }

    float CalculateFitness(IEntity member)
    {
        string text = member.Name;

        // Probably compiler generated types without meaningful names, show them last
        if (text.StartsWith("<"))
        {
            return 0;
        }

        // Constructors and destructors always have the same name in IL:
        // Use type name instead
        if (member.SymbolKind == SymbolKind.Constructor || member.SymbolKind == SymbolKind.Destructor)
        {
            text = member.DeclaringType?.Name ?? "";
        }

        // Ignore generic arguments, it not possible to search based on them either
        text = ReflectionHelper.SplitTypeParameterCountFromReflectionName(text);

        return 1.0f / text.Length;
    }

    string GetLanguageSpecificName(IEntity member)
    {
        switch (member)
        {
            case ITypeDefinition t:
                return language.TypeToString(t, false);
            case IField f:
                return language.FieldToString(f, true, false, false);
            case IProperty p:
                return language.PropertyToString(p, true, false, false);
            case IMethod m:
                return language.MethodToString(m, true, false, false);
            case IEvent e:
                return language.EventToString(e, true, false, false);
            default:
                throw new NotSupportedException(member?.GetType() + " not supported!");
        }
    }

    public MemberSearchResult Create(IEntity entity)
    {
        var declaringType = entity.DeclaringTypeDefinition;
        return new MemberSearchResult
        {
            Member = entity,
            Fitness = CalculateFitness(entity),
            Name = GetLanguageSpecificName(entity),
            Location = declaringType != null ? language.TypeToString(declaringType, includeNamespace: true) : entity.Namespace,
            Assembly = entity?.ParentModule?.MetadataFile?.FileName ?? "",
            ToolTip = entity?.ParentModule?.MetadataFile?.FileName,
            Image = new object(),
            LocationImage = new object(),
            AssemblyImage = new object(),
        };
    }

    public ResourceSearchResult Create(MetadataFile module, Resource resource, ITreeNode node, ITreeNode parent)
    {
        return new ResourceSearchResult
        {
            Resource = resource,
            Fitness = 1.0f / resource.Name.Length,
            Image = node.Icon,
            Name = resource.Name,
            LocationImage = parent.Icon,
            Location = (string) parent.Text,
            Assembly = module.FullName,
            ToolTip = module.FileName,
            AssemblyImage = new object(),
        };
    }

    public AssemblySearchResult Create(MetadataFile module)
    {
        return new AssemblySearchResult
        {
            Module = module,
            Fitness = 1.0f / module.Name.Length,
            Name = module.Name,
            Location = module.FileName,
            Assembly = module.FullName,
            ToolTip = module.FileName,
            Image = new object(),
            LocationImage = new object(),
            AssemblyImage = new object(),
        };
    }

    public NamespaceSearchResult Create(MetadataFile module, INamespace ns)
    {
        var name = ns.FullName.Length == 0 ? "-" : ns.FullName;
        return new NamespaceSearchResult
        {
            Namespace = ns,
            Name = name,
            Fitness = 1.0f / name.Length,
            Location = module.Name,
            Assembly = module.FullName,
            Image = new object(),
            LocationImage = new object(),
            AssemblyImage = new object(),
        };
    }
}