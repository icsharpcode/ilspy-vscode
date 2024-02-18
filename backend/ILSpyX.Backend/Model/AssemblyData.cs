// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpy.Backend.Model
{
    public record AssemblyData(
        string Name,
        string FilePath)
    {
        public string? Version { get; set; }
        public string? TargetFramework { get; set; }
    }
}
