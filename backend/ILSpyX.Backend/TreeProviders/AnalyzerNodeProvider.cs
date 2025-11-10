using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX;
using ICSharpCode.ILSpyX.Analyzers;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.TreeProviders;

public class AnalyzerNodeProvider(
    DecompilerBackend decompilerBackend,
    SingleThreadAssemblyList singleThreadAssemblyList,
    AnalyzerBackend analyzerBackend) : ITreeNodeProvider
{
    public Task<DecompileResult> Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return Task.FromResult(DecompileResult.Empty());
    }

    public async Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || nodeMetadata.Type != NodeType.Analyzer)
        {
            return [];
        }

        var analyzer = analyzerBackend.GetAnalyzerForNode(nodeMetadata)?.Instance;
        if (analyzer is null || singleThreadAssemblyList.AssemblyList is null)
        {
            return [];
        }

        var context = new AnalyzerContext
        {
            CancellationToken = CancellationToken.None,
            Language = new CSharpLanguage(),
            AssemblyList = singleThreadAssemblyList.AssemblyList
        };

        var nodeEntity = await decompilerBackend.GetEntityFromHandle(
            nodeMetadata.GetAssemblyFileIdentifier(), MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
        if (nodeEntity is null || !analyzer.Show(nodeEntity))
        {
            return [];
        }

        return
            analyzer.Analyze(nodeEntity, context)
                .OfType<IEntity>()
                .Select(entity => {
                    string nodeName = entity is IMethod method
                        ? method.MethodToString(false, false, false)
                        : entity.Name;
                    string location = (entity as IMember)?.DeclaringType.TypeToString(true) ?? "";

                    var assemblyFileIdentifier = entity.ParentModule?.MetadataFile?.GetAssemblyFileIdentifier();
                    if (assemblyFileIdentifier is null)
                    {
                        return null;
                    }
                    
                    return new Node
                    {
                        Metadata = new NodeMetadata
                        {
                            AssemblyPath = assemblyFileIdentifier.File,
                            BundleSubPath = assemblyFileIdentifier.BundleSubPath,
                            Type = NodeTypeHelper.GetNodeTypeFromEntity(entity),
                            Name = nodeName,
                            SymbolToken = MetadataTokens.GetToken(entity.MetadataToken),
                            ParentSymbolToken = entity.DeclaringTypeDefinition?.MetadataToken is not null
                                ? MetadataTokens.GetToken(entity.DeclaringTypeDefinition.MetadataToken)
                                : 0,
                            IsDecompilable = true
                        },
                        DisplayName = nodeName,
                        Description = location,
                        MayHaveChildren = false,
                        SymbolModifiers = NodeTypeHelper.GetSymbolModifiers(entity),
                        Flags = NodeFlagsHelper.GetNodeFlags(entity)
                    };
                }).OfType<Node>();
    }

    public async Task<Node?> CreateNode(NodeMetadata? nodeMetadata, AnalyzerInstance analyzer)
    {
        if (nodeMetadata is null)
        {
            return null;
        }

        var nodeEntity = await decompilerBackend.GetEntityFromHandle(
            nodeMetadata.GetAssemblyFileIdentifier(), MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
        if (nodeEntity is null || !analyzer.Instance.Show(nodeEntity))
        {
            return null;
        }

        string displayName = analyzer.Header;
        return new Node
        {
            Metadata = nodeMetadata with { Type = NodeType.Analyzer, SubType = analyzer.NodeSubType },
            DisplayName = displayName,
            Description = displayName,
            MayHaveChildren = true,
            SymbolModifiers = SymbolModifiers.None
        };
    }
}