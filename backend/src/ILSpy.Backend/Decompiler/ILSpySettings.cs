using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;

namespace ILSpy.Backend.Decompiler;

public class ILSpySettings
{
    private CSharpFormattingOptions formattingOptions;
    private DecompilerSettings decompilerSettings;

    public ILSpySettings()
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

