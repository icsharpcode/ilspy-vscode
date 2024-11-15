using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ILSpy.Backend.Model;

namespace ILSpyX.Backend.Application;

public class ILSpyBackendSettings
{
    private CSharpFormattingOptions formattingOptions;

    public ILSpyBackendSettings()
    {
        formattingOptions = FormattingOptionsFactory.CreateAllman();
        formattingOptions.IndentationString = "    ";
    }

    public DecompilerSettings CreateDecompilerSettings(string outputLanguage = LanguageName.CSharpLatest)
    {
        var decompilerSettings = new DecompilerSettings
        {
            ThrowOnAssemblyResolveErrors = false,
            CSharpFormattingOptions = formattingOptions
        };
        decompilerSettings.SetLanguageVersion(GetCSharpLanguageVersion(outputLanguage));
        return decompilerSettings;
    }

    private LanguageVersion GetCSharpLanguageVersion(string languageName)
    {
        return languageName switch
        {
            LanguageName.CSharp_1 => LanguageVersion.CSharp1,
            LanguageName.CSharp_2 => LanguageVersion.CSharp2,
            LanguageName.CSharp_3 => LanguageVersion.CSharp3,
            LanguageName.CSharp_4 => LanguageVersion.CSharp4,
            LanguageName.CSharp_5 => LanguageVersion.CSharp5,
            LanguageName.CSharp_6 => LanguageVersion.CSharp6,
            LanguageName.CSharp_7 => LanguageVersion.CSharp7,
            LanguageName.CSharp_7_1 => LanguageVersion.CSharp7_1,
            LanguageName.CSharp_7_2 => LanguageVersion.CSharp7_2,
            LanguageName.CSharp_7_3 => LanguageVersion.CSharp7_3,
            LanguageName.CSharp_8 => LanguageVersion.CSharp8_0,
            LanguageName.CSharp_9 => LanguageVersion.CSharp9_0,
            LanguageName.CSharp_10 => LanguageVersion.CSharp10_0,
            LanguageName.CSharp_11 => LanguageVersion.CSharp11_0,
            _ => LanguageVersion.CSharp11_0,
        };
    }
}

