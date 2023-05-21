using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace ILSpy.Backend.Decompiler;

public class ILSpyBackendSettings
{
    private CSharpFormattingOptions formattingOptions;
    private DecompilerSettings decompilerSettings;

    public ILSpyBackendSettings()
    {
        formattingOptions = FormattingOptionsFactory.CreateAllman();
        formattingOptions.IndentationString = "    ";

        decompilerSettings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            CSharpFormattingOptions = formattingOptions
        };
    }


    public DecompilerSettings DecompilerSettings => decompilerSettings;
}

