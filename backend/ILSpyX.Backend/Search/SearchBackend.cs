// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Search;
using ILSpy.Backend.Decompiler;
using ILSpy.Backend.Model;
using ILSpy.Backend.TreeProviders;
using ILSpyX.Backend.Application;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Search;

public class SearchBackend
{
    private readonly ILogger logger;
    private readonly IComparer<SearchResult> resultsComparer = SearchResult.ComparerByName;
    private readonly SingleThreadAssemblyList assemblyList;
    private readonly ILSpyBackendSettings ilspyBackendSettings;

    public SearchBackend(ILoggerFactory loggerFactory, SingleThreadAssemblyList assemblyList, ILSpyBackendSettings ilspyBackendSettings)
    {
        logger = loggerFactory.CreateLogger<SearchBackend>();
        this.assemblyList = assemblyList;
        this.ilspyBackendSettings = ilspyBackendSettings;
    }

    public async Task AddAssembly(string path)
    {
        await assemblyList.AddAssembly(path);
    }

    public async Task RemoveAssembly(string path)
    {
        await assemblyList.RemoveAssembly(path);
    }

    public async Task<IEnumerable<Node>> Search(string searchTerm, CancellationToken cancellationToken)
    {
        if (!string.IsNullOrEmpty(searchTerm))
        {
            try
            {
                var assemblies = await assemblyList.GetAllAssemblies();

                var resultQueue = new ConcurrentQueue<SearchResult>();
                var searchRequest = CreateSearchRequest(searchTerm, SearchMode.TypeAndMember);

                await Task.Factory.StartNew(() => {
                    var searcher = new MemberSearchStrategy(new CSharpLanguage(), ApiVisibility.All, searchRequest, resultQueue);
                    if (searcher == null)
                        return;
                    try
                    {
                        foreach (var loadedAssembly in assemblies)
                        {
                            var module = loadedAssembly.GetMetadataFileOrNull();
                            if (module == null)
                                continue;
                            searcher.Search(module, cancellationToken);
                        }
                    }
                    catch (OperationCanceledException)
                    {
                        // ignore cancellation
                    }

                }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

                return resultQueue.Where(IsNotAccessor).OrderBy(r => r, resultsComparer).Select(ConvertResultToNode);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation
            }
        }

        return Enumerable.Empty<Node>();
    }

    bool IsNotAccessor(SearchResult searchResult)
    {
        if (searchResult is MemberSearchResult memberSearchResult)
        {
            return memberSearchResult.Member.SymbolKind != SymbolKind.Accessor;
        }

        return true;
    }

    Node ConvertResultToNode(SearchResult result)
    {
        var memberSearchResult = result as MemberSearchResult;
        return new Node(
            Metadata: new NodeMetadata(
                AssemblyPath: result.Assembly,
                Type: GetNodeType(result),
                Name: memberSearchResult?.Member?.Name ?? result.Name,
                SymbolToken: memberSearchResult != null ? MetadataTokens.GetToken(memberSearchResult.Member.MetadataToken) : 0,
                ParentSymbolToken:
                    memberSearchResult?.Member.DeclaringTypeDefinition?.MetadataToken != null ?
                    MetadataTokens.GetToken(memberSearchResult.Member.DeclaringTypeDefinition.MetadataToken) : 0),
            DisplayName: result.Name,
            Description: result.Location,
            MayHaveChildren: memberSearchResult?.Member is ITypeDefinition,
            SymbolModifiers: GetSymbolModifiers(result));
    }

    NodeType GetNodeType(SearchResult result) => result switch
    {
        AssemblySearchResult => NodeType.Assembly,
        NamespaceSearchResult => NodeType.Namespace,
        MemberSearchResult msr => msr.Member switch
        {
            ITypeDefinition typeDefinition => NodeTypeHelper.GetNodeTypeFromTypeKind(typeDefinition.Kind),
            IMethod => NodeType.Method,
            IField => NodeType.Field,
            IEvent => NodeType.Event,
            IProperty => NodeType.Property,
            _ => NodeType.Unknown
        },
        _ => NodeType.Unknown
    };

    SymbolModifiers GetSymbolModifiers(SearchResult result)
    {
        var modifiers = SymbolModifiers.None;
        switch (result)
        {
            case MemberSearchResult memberSearchResult:
                switch (memberSearchResult.Member)
                {
                    case ITypeDefinition typeDefinition:
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, typeDefinition.IsAbstract);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Static, typeDefinition.IsStatic);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, typeDefinition.IsReadOnly);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, typeDefinition.IsSealed);
                        break;
                    case IField field:
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, field.IsAbstract);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, field.IsVirtual);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Override, field.IsOverride);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Static, field.IsStatic);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, field.IsSealed);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, field.IsReadOnly);
                        break;
                    case IMember member:
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, member.IsAbstract);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, member.IsVirtual);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Override, member.IsOverride);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Static, member.IsStatic);
                        NodeTypeHelper.MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, member.IsSealed);
                        break;
                }
                NodeTypeHelper.MapSymbolModifierFromAccessibility(ref modifiers, memberSearchResult.Member.Accessibility);
                break;
        }
        return modifiers;
    }

    SearchRequest CreateSearchRequest(string input, SearchMode searchMode)
    {
        string[] parts = input.Split(' '); // NativeMethods.CommandLineToArgumentArray(input);

        var request = new SearchRequest();
        var keywords = new List<string>();
        Regex? regex = null;
        request.Mode = searchMode;

        foreach (string part in parts)
        {
            // Parse: [prefix:|@]["]searchTerm["]
            // Find quotes used for escaping
            int prefixLength = part.IndexOfAny(new[] { '"', '/' });
            if (prefixLength < 0)
            {
                // no quotes
                prefixLength = part.Length;
            }

            // Find end of prefix
            if (part.StartsWith("@", StringComparison.Ordinal))
            {
                prefixLength = 1;
            }
            else
            {
                prefixLength = part.IndexOf(':', 0, prefixLength);
            }
            string? prefix;
            if (prefixLength <= 0)
            {
                prefix = null;
                prefixLength = -1;
            }
            else
            {
                prefix = part.Substring(0, prefixLength);
            }

            // unescape quotes
            string searchTerm = part.Substring(prefixLength + 1).Trim();
            if (searchTerm.Length > 0)
            {
                searchTerm = searchTerm.Split(' ').First(); // NativeMethods.CommandLineToArgumentArray(searchTerm)[0];
            }

            if (prefix == null || prefix.Length <= 2)
            {
                if (regex == null && searchTerm.StartsWith("/", StringComparison.Ordinal) && searchTerm.Length > 1)
                {
                    int searchTermLength = searchTerm.Length - 1;
                    if (searchTerm.EndsWith("/", StringComparison.Ordinal))
                    {
                        searchTermLength--;
                    }

                    request.FullNameSearch |= searchTerm.Contains("\\.");

                    regex = CreateRegex(searchTerm.Substring(1, searchTermLength));
                }
                else
                {
                    request.FullNameSearch |= searchTerm.Contains(".");
                    keywords.Add(searchTerm);
                }
                request.OmitGenerics |= !(searchTerm.Contains("<") || searchTerm.Contains("`"));
            }

            switch (prefix?.ToUpperInvariant())
            {
                case "@":
                    request.Mode = SearchMode.Token;
                    break;
                case "INNAMESPACE":
                    request.InNamespace ??= searchTerm;
                    break;
                case "INASSEMBLY":
                    request.InAssembly ??= searchTerm;
                    break;
                case "A":
                    request.AssemblySearchKind = AssemblySearchKind.NameOrFileName;
                    request.Mode = SearchMode.Assembly;
                    break;
                case "AF":
                    request.AssemblySearchKind = AssemblySearchKind.FilePath;
                    request.Mode = SearchMode.Assembly;
                    break;
                case "AN":
                    request.AssemblySearchKind = AssemblySearchKind.FullName;
                    request.Mode = SearchMode.Assembly;
                    break;
                case "N":
                    request.Mode = SearchMode.Namespace;
                    break;
                case "TM":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.All;
                    break;
                case "T":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Type;
                    break;
                case "M":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Member;
                    break;
                case "MD":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Method;
                    break;
                case "F":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Field;
                    break;
                case "P":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Property;
                    break;
                case "E":
                    request.Mode = SearchMode.Member;
                    request.MemberSearchKind = MemberSearchKind.Event;
                    break;
                case "C":
                    request.Mode = SearchMode.Literal;
                    break;
                case "R":
                    request.Mode = SearchMode.Resource;
                    break;
            }
        }

        Regex? CreateRegex(string s)
        {
            try
            {
                return new Regex(s, RegexOptions.Compiled);
            }
            catch (ArgumentException)
            {
                return null;
            }
        }

        request.Keywords = keywords.ToArray();
        if (regex != null)
        {
            request.RegEx = regex;
        }
        request.SearchResultFactory = new SearchResultFactory();
        request.DecompilerSettings = ilspyBackendSettings.CreateDecompilerSettings();

        return request;
    }
}
