// Copyright (c) 2022 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project

namespace ILSpy.Backend.Decompiler;

using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpy.Search;
using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Search;
using ILSpy.Backend.Model;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

public class SearchBackend
{
    private readonly ILogger logger;
    private readonly AssemblyList assemblyList;
    private readonly IComparer<SearchResult> resultsComparer = SearchResult.ComparerByName;
    private readonly AssemblyListManager assemblyListManager;

    public SearchBackend(ILoggerFactory loggerFactory, AssemblyListManager assemblyListManager)
    {
        logger = loggerFactory.CreateLogger<SearchBackend>();
        this.assemblyListManager = assemblyListManager;
        assemblyList = assemblyListManager.LoadList(AssemblyListManager.DefaultListName);
    }

    public void AddAssembly(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            assemblyList.Open(path);
            assemblyListManager.SaveList(assemblyList);
        }
    }

    public void RemoveAssembly(string? path)
    {
        if (!string.IsNullOrEmpty(path))
        {
            var assembly = assemblyList.FindAssembly(path);
            if (assembly != null)
            {
                assemblyList.Unload(assembly);
            }
        }
    }

    public async Task<IEnumerable<NodeData>> Search(string searchTerm, CancellationToken cancellationToken)
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
                            var module = loadedAssembly.GetPEFileOrNull();
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

                return resultQueue.OrderBy(r => r, resultsComparer).Select(ConvertResultToNode);
            }
            catch (TaskCanceledException)
            {
                // ignore cancellation
            }
        }

        return Enumerable.Empty<NodeData>();
    }

    NodeData ConvertResultToNode(SearchResult result)
    {
        return new NodeData(
            Node: new Node(
                Type: GetNodeType(result),
                SymbolToken: 0,
                ParentSymbolToken: 0),
            Name: result.Name,
            Description: result.Location,
            MayHaveChildren: (result as MemberSearchResult)?.Member is ITypeDefinition,
            SymbolModifiers: GetSymbolModifiers(result));
    }

    NodeType GetNodeType(SearchResult result) => result switch
    {
        AssemblySearchResult => NodeType.Assembly,
        NamespaceSearchResult => NodeType.Namespace,
        MemberSearchResult msr => msr.Member switch
        {
            ITypeDefinition typeDefinition => typeDefinition.Kind switch
            {
                TypeKind.Class => NodeType.Class,
                TypeKind.Delegate => NodeType.Delegate,
                TypeKind.Enum => NodeType.Enum,
                TypeKind.Interface => NodeType.Interface,
                TypeKind.Struct => NodeType.Struct,
                _ => NodeType.Unknown
            },
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
        SymbolModifiers modifiers = SymbolModifiers.None;

        switch (result)
        {
            case ITypeDefinition typeDefinition:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, typeDefinition.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, typeDefinition.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, typeDefinition.IsReadOnly);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, typeDefinition.IsSealed);
                break;

            case IField field:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, field.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, field.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, field.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, field.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, field.IsSealed);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, field.IsReadOnly);
                break;

            case IMember member:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, member.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, member.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, member.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, member.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, member.IsSealed);
                break;
        }

        return modifiers;
    }

    private void MapSymbolModifier(ref SymbolModifiers modifiers, SymbolModifiers modifier, bool condition)
    {
        if (condition)
        {
            modifiers |= modifier;
        }
    }

    SearchRequest CreateSearchRequest(string input, SearchMode searchMode)
    {
        string[] parts = input.Split(' '); // NativeMethods.CommandLineToArgumentArray(input);

        SearchRequest request = new SearchRequest();
        List<string> keywords = new List<string>();
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
        request.RegEx = regex;
        request.SearchResultFactory = new SearchResultFactory();
        //request.TreeNodeFactory = new TreeNodeFactory();
        request.DecompilerSettings = new DecompilerSettings();

        return request;
    }
}
