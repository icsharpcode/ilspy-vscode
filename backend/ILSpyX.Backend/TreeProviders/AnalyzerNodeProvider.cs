using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Analyzers;
using ILSpyX.Backend.Analyzers;
using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;
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
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Task<IEnumerable<Node>> GetChildrenAsync(NodeMetadata? nodeMetadata)
    {
        if (nodeMetadata is null || nodeMetadata.Type != NodeType.Analyzer)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        var analyzer = analyzerBackend.GetAnalyzerForNode(nodeMetadata)?.Instance;
        if (analyzer is null)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        if (singleThreadAssemblyList.AssemblyList is null)
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        var context = new AnalyzerContext
        {
            CancellationToken = CancellationToken.None,
            Language = new CSharpLanguage(),
            AssemblyList = singleThreadAssemblyList.AssemblyList
        };

        var nodeEntity = decompilerBackend.GetEntityFromHandle(
            nodeMetadata.AssemblyPath, MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
        if (nodeEntity is null || !analyzer.Show(nodeEntity))
        {
            return Task.FromResult(Enumerable.Empty<Node>());
        }

        return Task.FromResult(
            analyzer.Analyze(nodeEntity, context)
                .OfType<IEntity>()
                .Select(entity => {
                    string nodeName = entity is IMethod method
                        ? method.MethodToString(false, false, false)
                        : entity.Name;
                    string location = (entity as IMember)?.DeclaringType.TypeToString(true) ?? "";
                    return new Node(
                        new NodeMetadata(
                            entity.ParentModule?.MetadataFile?.FileName ?? "",
                            NodeTypeHelper.GetNodeTypeFromEntity(entity),
                            nodeName,
                            MetadataTokens.GetToken(entity.MetadataToken),
                            entity.DeclaringTypeDefinition?.MetadataToken is not null
                                ? MetadataTokens.GetToken(entity.DeclaringTypeDefinition.MetadataToken)
                                : 0,
                            IsDecompilable: true),
                        nodeName,
                        location,
                        false,
                        NodeTypeHelper.GetSymbolModifiers(entity),
                        NodeFlagsHelper.GetNodeFlags(entity)
                    );
                })
        );
    }

    public Node? CreateNode(NodeMetadata? nodeMetadata, AnalyzerInstance analyzer)
    {
        if (nodeMetadata is null)
        {
            return null;
        }

        var nodeEntity = decompilerBackend.GetEntityFromHandle(
            nodeMetadata.AssemblyPath, MetadataTokens.EntityHandle(nodeMetadata.SymbolToken));
        if (nodeEntity is null || !analyzer.Instance.Show(nodeEntity))
        {
            return null;
        }

        string displayName = analyzer.Header;
        return new Node(
            nodeMetadata with { Type = NodeType.Analyzer, SubType = analyzer.NodeSubType },
            displayName,
            displayName,
            true,
            SymbolModifiers.None
        );
    }
}