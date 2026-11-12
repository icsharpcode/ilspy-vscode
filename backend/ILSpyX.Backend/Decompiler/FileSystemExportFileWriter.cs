// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public sealed class FileSystemExportFileWriter : IExportFileWriter
{
    public async Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken)
    {
        string? directory = Path.GetDirectoryName(path);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        await File.WriteAllTextAsync(path, contents, Encoding.UTF8, cancellationToken);
    }
}