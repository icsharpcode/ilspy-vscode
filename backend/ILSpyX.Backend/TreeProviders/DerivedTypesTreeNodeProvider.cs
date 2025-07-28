using ILSpyX.Backend.Decompiler;
using ILSpyX.Backend.Model;

namespace ILSpyX.Backend.TreeProviders;

public class DerivedTypesNodeProvider : ITreeNodeProvider
{
    public DecompileResult Decompile(NodeMetadata nodeMetadata, string outputLanguage)
    {
        return DecompileResult.Empty();
    }

    public Node CreateNode(string assemblyPath, int typeSymbolToken)
    {
        return new Node(
            new NodeMetadata(
                assemblyPath,
                NodeType.DerivedTypes,
                "Derived Types",
                0,
                typeSymbolToken),
            "Derived Types",
            string.Empty,
            true
        );
    }
}