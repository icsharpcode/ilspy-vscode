// Copyright (c) 2026 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System.Threading;
using System.Threading.Tasks;

namespace ILSpyX.Backend.Decompiler;

public interface IExportFileWriter
{
    Task WriteTextAsync(string path, string contents, CancellationToken cancellationToken);
}