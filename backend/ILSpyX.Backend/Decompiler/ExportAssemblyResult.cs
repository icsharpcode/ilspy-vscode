// Copyright (c) 2025 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpyX.Backend.Decompiler;

public record ExportAssemblyResult(
    bool Succeeded,
    string? OutputDirectory,
    int FilesWritten,
    int ErrorCount,
    string? ErrorMessage);
