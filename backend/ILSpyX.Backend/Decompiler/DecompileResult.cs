namespace ILSpy.Backend.Decompiler;

public class DecompileResult
{
    public string? DecompiledCode { get; init; }
    public bool IsError { get; init; }
    public string? ErrorMessage { get; init; }

    private DecompileResult()
    {
    }

    public static DecompileResult Empty() => new()
    {
        DecompiledCode = null,
        IsError = false
    };

    public static DecompileResult WithCode(string? decompiledCode) => new()
    {
        DecompiledCode = decompiledCode,
        IsError = false
    };

    public static DecompileResult WithError(string? errorMessage) => new()
    {
        IsError = true,
        ErrorMessage = errorMessage
    };
}