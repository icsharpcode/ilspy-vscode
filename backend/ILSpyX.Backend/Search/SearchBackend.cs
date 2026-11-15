// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Search;
using ILSpyX.Backend.Application;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using ILSpyX.Backend.TreeProviders;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Search;

public partial class SearchBackend(
    SingleThreadAssemblyList assemblyList,
    ILSpyBackendSettings ilspyBackendSettings,
    TreeNodeProviders treeNodeProviders)
{
    // Locating a match requires decompiling the result, so limit it for searches with many results.
    private const int MaxLocatedMatches = 100;

    private readonly IComparer<SearchResult> resultsComparer = SearchResult.ComparerByName;

    /// <summary>
    /// Searches all loaded assemblies. If an output language is given, results found by a literal
    /// search get the location of the literal in their decompiled code in that language.
    /// </summary>
    public async Task<IEnumerable<SearchResultNode>> Search(string searchTerm, CancellationToken cancellationToken,
        string? outputLanguage = null)
    {
        if (string.IsNullOrEmpty(searchTerm))
        {
            return [];
        }

        try
        {
            var addedAssemblies = (await assemblyList.GetMetadataFileAssemblies())
                .Where(assembly => !assembly.IsAutoLoaded).ToList();

            var resultQueue = new ConcurrentQueue<SearchResult>();
            var literalResultQueue = new ConcurrentQueue<SearchResult>();
            var searchRequest = CreateSearchRequest(searchTerm, SearchMode.TypeAndMember, out bool searchAll);
            var searchers = searchAll
                ? CreateSearchAllStrategies(searchRequest, resultQueue, literalResultQueue)
                : CreateSearchStrategy(searchRequest, resultQueue, literalResultQueue);
            if (searchers.Any(searcher => searcher is null))
            {
                return [];
            }

            await Task.Factory.StartNew(() => {
                try
                {
                    foreach (var loadedAssembly in WithDependencies(addedAssemblies, cancellationToken))
                    {
                        var module = loadedAssembly.GetMetadataFileOrNull();
                        if (module == null)
                        {
                            continue;
                        }

                        foreach (var searcher in searchers)
                        {
                            searcher!.Search(module, cancellationToken);
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // ignore cancellation
                }
            }, cancellationToken, TaskCreationOptions.LongRunning, TaskScheduler.Current).ConfigureAwait(false);

            var literalResults = new HashSet<SearchResult>(literalResultQueue);
            var literalResultNodes = new HashSet<NodeMetadata>();
            var nodes = new List<Node>();
            foreach (var result in resultQueue.Concat(literalResultQueue).Where(IsNotAccessor)
                         .OrderBy(r => r, resultsComparer))
            {
                if (await ConvertResultToNode(result) is { } node)
                {
                    nodes.Add(node);
                    if (literalResults.Contains(result) && node.Metadata is not null)
                    {
                        literalResultNodes.Add(node.Metadata);
                    }
                }
            }

            // In "all:" mode the same member can be found by name and by a literal it contains.
            var resultNodes = new List<SearchResultNode>();
            int matchesToLocate = MaxLocatedMatches;
            foreach (var node in nodes.DistinctBy(node => node.Metadata))
            {
                var resultNode = new SearchResultNode(node);
                bool isLiteralMatch = node.Metadata is not null && literalResultNodes.Contains(node.Metadata);
                // Members and types found by name: locate the name, usually in the declaration.
                bool isNameMatch = !isLiteralMatch && node.Metadata is { SymbolToken: not 0 } &&
                                   searchRequest.Mode != SearchMode.Token;
                if (outputLanguage is not null && (isLiteralMatch || isNameMatch) && matchesToLocate-- > 0)
                {
                    cancellationToken.ThrowIfCancellationRequested();
                    resultNode = resultNode with
                    {
                        Match = await LocateMatch(node.Metadata!, outputLanguage, searchRequest, isNameMatch)
                    };
                }

                resultNodes.Add(resultNode);
            }

            return resultNodes;
        }
        catch (OperationCanceledException)
        {
            // ignore cancellation
        }

        return [];
    }

    /// <summary>
    /// Lists the given assemblies and their dependencies, recursively. Dependencies, which are part of the
    /// .NET framework, are skipped: they would slow down the search and flood the results.
    /// </summary>
    private static IEnumerable<LoadedAssembly> WithDependencies(IEnumerable<LoadedAssembly> assemblies,
        CancellationToken cancellationToken)
    {
        var visited = new HashSet<LoadedAssembly>(assemblies);
        var queue = new Queue<LoadedAssembly>(visited);
        while (queue.TryDequeue(out var assembly))
        {
            cancellationToken.ThrowIfCancellationRequested();
            yield return assembly;

            var module = assembly.GetMetadataFileOrNull();
            if (module is null)
            {
                continue;
            }

            var resolver = assembly.GetAssemblyResolver();
            foreach (var reference in module.AssemblyReferences)
            {
                LoadedAssembly? dependency;
                try
                {
                    dependency = resolver.Resolve(reference)?.GetLoadedAssembly();
                }
                catch (Exception)
                {
                    continue;
                }

                if (dependency is not null && !IsFrameworkAssembly(dependency.FileName) && visited.Add(dependency))
                {
                    queue.Enqueue(dependency);
                }
            }
        }
    }

    private static bool IsFrameworkAssembly(string fileName) => FrameworkPathRegex().IsMatch(fileName);

    // .NET (Core) shared frameworks and reference packs, .NET Framework and Mono installations.
    [GeneratedRegex(
        @"[\\/](shared[\\/]Microsoft\.[^\\/]+\.App|packs[\\/][^\\/]+\.Ref|Microsoft\.NET[\\/](Framework|Framework64|assembly)|Reference Assemblies[\\/]Microsoft[\\/]Framework|mono[\\/][0-9][^\\/]*)[\\/]",
        RegexOptions.IgnoreCase)]
    private static partial Regex FrameworkPathRegex();

    /// <summary>
    /// Finds the first line of the node's decompiled code containing the search term.
    /// </summary>
    private async Task<SearchMatch?> LocateMatch(NodeMetadata nodeMetadata, string outputLanguage,
        SearchRequest request, bool isNameMatch)
    {
        string? code;
        try
        {
            code = (await treeNodeProviders.ForNode(nodeMetadata).Decompile(nodeMetadata, outputLanguage))
                .DecompiledCode;
        }
        catch (Exception)
        {
            return null;
        }

        string[] lines = (code?.Split('\n') ?? []).Select(line => line.TrimEnd('\r')).ToArray();
        string[] keywords = request.Keywords.Where(keyword => keyword.Length > 0)
            // Qualified names (e.g. "Type.Member") don't appear as is in the code, but their last part does.
            .Select(keyword => isNameMatch && keyword.LastIndexOf('.') is > 0 and var dot && dot < keyword.Length - 1
                ? keyword[(dot + 1)..]
                : keyword)
            .ToArray();
        // Prefer number 401 itself over 401 in "1401" or in identifiers like Status401, in any line.
        foreach (bool wholeNumbersOnly in new[] { true, false })
        {
            for (int lineIndex = 0; lineIndex < lines.Length; lineIndex++)
            {
                string line = lines[lineIndex];
                // Using directives contain neither declarations nor literals.
                if (line.TrimStart().StartsWith("using ", StringComparison.Ordinal) &&
                    line.TrimEnd().EndsWith(';'))
                {
                    continue;
                }

                if (FindInLine(line, request.RegEx, keywords, wholeNumbersOnly) is { } match)
                {
                    return new SearchMatch(lineIndex, match.Column, match.Length,
                        line.Substring(match.Column, match.Length));
                }
            }
        }

        return null;
    }

    private static (int Column, int Length)? FindInLine(string line, Regex? regex, string[] keywords,
        bool wholeNumbersOnly)
    {
        if (regex is not null)
        {
            var match = regex.Match(line);
            return match is { Success: true, Length: > 0 } ? (match.Index, match.Length) : null;
        }

        foreach (string keyword in keywords)
        {
            bool isNumber = keyword.All(char.IsDigit);
            for (int index = line.IndexOf(keyword, StringComparison.OrdinalIgnoreCase);
                 index >= 0;
                 index = line.IndexOf(keyword, index + 1, StringComparison.OrdinalIgnoreCase))
            {
                bool isWholeNumber = (index == 0 || !IsIdentifierChar(line[index - 1])) &&
                                     (index + keyword.Length == line.Length ||
                                      !char.IsDigit(line[index + keyword.Length]));
                if (!wholeNumbersOnly || !isNumber || isWholeNumber)
                {
                    return (index, keyword.Length);
                }
            }
        }

        return null;

        static bool IsIdentifierChar(char c) => char.IsLetterOrDigit(c) || c == '_';
    }

    /// <summary>
    /// Picks the search strategy for the mode selected by the search term's prefix (e.g. "c:" for literals),
    /// mirroring ILSpy's search pane. Resource search ("r:") is not supported yet, because embedded resources
    /// are not represented as nodes.
    /// </summary>
    private static List<AbstractSearchStrategy?> CreateSearchStrategy(SearchRequest request,
        IProducerConsumerCollection<SearchResult> resultQueue,
        IProducerConsumerCollection<SearchResult> literalResultQueue)
    {
        var language = new CSharpLanguage();
        return request.Mode switch
        {
            SearchMode.TypeAndMember => [new MemberSearchStrategy(language, ApiVisibility.All, request, resultQueue)],
            SearchMode.Member =>
            [
                new MemberSearchStrategy(language, ApiVisibility.All, request, resultQueue, request.MemberSearchKind)
            ],
            SearchMode.Literal => CreateLiteralSearchStrategies(request, literalResultQueue),
            SearchMode.Token =>
                [new MetadataTokenSearchStrategy(language, ApiVisibility.All, request, resultQueue)],
            SearchMode.Assembly => [new AssemblySearchStrategy(request, resultQueue, request.AssemblySearchKind)],
            SearchMode.Namespace => [new NamespaceSearchStrategy(request, resultQueue)],
            _ => [null]
        };
    }

    /// <summary>
    /// ILSpy searches a single number term (e.g. "401") only as numeric constant, so string literals
    /// containing it (e.g. "401") are not found. Search number terms in strings, too.
    /// </summary>
    private static List<AbstractSearchStrategy?> CreateLiteralSearchStrategies(SearchRequest request,
        IProducerConsumerCollection<SearchResult> literalResultQueue)
    {
        var language = new CSharpLanguage();
        var strategies = new List<AbstractSearchStrategy?>
        {
            new LiteralSearchStrategy(language, ApiVisibility.All, request, literalResultQueue)
        };

        string[] terms = request.Keywords.Where(keyword => keyword.Length > 0).ToArray();
        if (request.RegEx is null && terms is [var term] && NumberLiteralRegex().IsMatch(term))
        {
            // With more than one term, the literal strategy doesn't parse the term as a number and finds
            // strings containing all terms - so repeating the term finds strings containing it.
            var textRequest = request; // SearchRequest is a struct, this is a copy
            textRequest.Keywords = [term, term];
            strategies.Add(new LiteralSearchStrategy(language, ApiVisibility.All, textRequest, literalResultQueue));
        }

        return strategies;
    }

    [GeneratedRegex(@"^[+-]?(0x[0-9a-f]+|[0-9]+(\.[0-9]+)?)[a-z]*$", RegexOptions.IgnoreCase)]
    private static partial Regex NumberLiteralRegex();

    /// <summary>
    /// Strategies for the "all:" prefix: types and members, literals, namespaces and assemblies at once.
    /// </summary>
    private static List<AbstractSearchStrategy?> CreateSearchAllStrategies(SearchRequest request,
        IProducerConsumerCollection<SearchResult> resultQueue,
        IProducerConsumerCollection<SearchResult> literalResultQueue)
    {
        var language = new CSharpLanguage();
        return
        [
            new MemberSearchStrategy(language, ApiVisibility.All, request, resultQueue),
            ..CreateLiteralSearchStrategies(request, literalResultQueue),
            new NamespaceSearchStrategy(request, resultQueue),
            new AssemblySearchStrategy(request, resultQueue, AssemblySearchKind.NameOrFileName)
        ];
    }

    private bool IsNotAccessor(SearchResult searchResult)
    {
        if (searchResult is MemberSearchResult memberSearchResult)
        {
            return memberSearchResult.Member.SymbolKind != SymbolKind.Accessor;
        }

        return true;
    }

    private async Task<Node?> ConvertResultToNode(SearchResult result)
    {
        return result switch
        {
            MemberSearchResult memberSearchResult => ConvertMemberSearchResultToNode(memberSearchResult),
            AssemblySearchResult assemblySearchResult => await ConvertAssemblySearchResultToNode(assemblySearchResult),
            NamespaceSearchResult namespaceSearchResult => ConvertNamespaceSearchResultToNode(namespaceSearchResult),
            _ => null
        };
    }

    private static async Task<Node?> ConvertAssemblySearchResultToNode(AssemblySearchResult assemblySearchResult)
    {
        var loadedAssembly = assemblySearchResult.Module.GetLoadedAssembly();
        if (loadedAssembly is null)
        {
            return null;
        }

        var assemblyData = await AssemblyUtility.CreateAssemblyDataAsync(loadedAssembly);
        return assemblyData is null ? null : AssemblyNodeProvider.CreateNode(assemblyData);
    }

    private static Node? ConvertNamespaceSearchResultToNode(NamespaceSearchResult namespaceSearchResult)
    {
        var assemblyFileIdentifier = namespaceSearchResult.Namespace.ContributingModules
            .Select(module => module.MetadataFile?.GetAssemblyFileIdentifier())
            .FirstOrDefault(identifier => identifier is not null);
        if (assemblyFileIdentifier is null)
        {
            return null;
        }

        return NamespaceNodeProvider.CreateNode(assemblyFileIdentifier, namespaceSearchResult.Namespace.FullName) with
        {
            DisplayName = namespaceSearchResult.Name,
            Description = namespaceSearchResult.Location
        };
    }

    private Node? ConvertMemberSearchResultToNode(MemberSearchResult memberSearchResult)
    {
        var assemblyFileIdentifier = memberSearchResult.Member.ParentModule?.MetadataFile?.GetAssemblyFileIdentifier();
        if (assemblyFileIdentifier is null)
        {
            return null;
        }

        return new Node
        {
            Metadata = new NodeMetadata
            {
                AssemblyPath = assemblyFileIdentifier.File,
                BundledAssemblyName = assemblyFileIdentifier.BundledAssemblyFile,
                Type = GetNodeType(memberSearchResult),
                Name = memberSearchResult.Member?.Name ?? memberSearchResult.Name,
                SymbolToken = memberSearchResult.Member is not null
                    ? MetadataTokens.GetToken(memberSearchResult.Member.MetadataToken)
                    : 0,
                ParentSymbolToken = memberSearchResult.Member?.DeclaringTypeDefinition?.MetadataToken != null
                    ? MetadataTokens.GetToken(memberSearchResult.Member.DeclaringTypeDefinition.MetadataToken)
                    : 0,
                AvailableCommands = AvailableNodeCommands.Decompile | AvailableNodeCommands.Analyze
            },
            DisplayName = memberSearchResult.Name,
            Description = memberSearchResult.Location,
            MayHaveChildren = memberSearchResult.Member is ITypeDefinition,
            SymbolModifiers = GetSymbolModifiers(memberSearchResult),
            Flags = NodeFlagsHelper.GetNodeFlags(memberSearchResult)
        };
    }

    private static NodeType GetNodeType(SearchResult result) =>
        result switch
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

    private SymbolModifiers GetSymbolModifiers(SearchResult result)
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

    private SearchRequest CreateSearchRequest(string input, SearchMode searchMode, out bool searchAll)
    {
        searchAll = false;
        string[] parts = input.Split(' '); // NativeMethods.CommandLineToArgumentArray(input);

        var request = new SearchRequest();
        var keywords = new List<string>();
        Regex? regex = null;
        request.Mode = searchMode;

        foreach (string part in parts)
        {
            // Parse: [prefix:|@]["]searchTerm["]
            // Find quotes used for escaping
            int prefixLength = part.IndexOfAny(['"', '/']);
            if (prefixLength < 0)
            {
                // no quotes
                prefixLength = part.Length;
            }

            // Find end of prefix
            prefixLength = part.StartsWith('@') ? 1 : part.IndexOf(':', 0, prefixLength);
            string? prefix;
            if (prefixLength <= 0)
            {
                prefix = null;
                prefixLength = -1;
            }
            else
            {
                prefix = part[..prefixLength];
            }

            // unescape quotes
            string searchTerm = part[(prefixLength + 1)..].Trim();
            if (searchTerm.Length > 0)
            {
                searchTerm = searchTerm.Split(' ').First(); // NativeMethods.CommandLineToArgumentArray(searchTerm)[0];
            }

            // Longer prefixes (e.g. "inassembly:") are filters, not search terms - except "all:".
            bool isSearchAllPrefix = string.Equals(prefix, "all", StringComparison.OrdinalIgnoreCase);
            if (prefix is not { Length: > 2 } || isSearchAllPrefix)
            {
                if (regex == null && searchTerm.StartsWith('/') && searchTerm.Length > 1)
                {
                    int searchTermLength = searchTerm.Length - 1;
                    if (searchTerm.EndsWith('/'))
                    {
                        searchTermLength--;
                    }

                    request.FullNameSearch |= searchTerm.Contains("\\.");

                    regex = CreateRegex(searchTerm.Substring(1, searchTermLength));
                }
                else
                {
                    request.FullNameSearch |= searchTerm.Contains('.');
                    keywords.Add(searchTerm);
                }

                request.OmitGenerics |= !(searchTerm.Contains('<') || searchTerm.Contains('`'));
            }

            switch (prefix?.ToUpperInvariant())
            {
                case "@":
                    request.Mode = SearchMode.Token;
                    break;
                case "ALL":
                    searchAll = true;
                    request.Mode = SearchMode.TypeAndMember;
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
