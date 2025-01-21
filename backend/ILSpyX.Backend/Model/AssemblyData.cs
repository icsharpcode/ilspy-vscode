// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpyX.Backend.Model
{
    public record AssemblyData(
        string Name,
        string FilePath,
        bool IsAutoLoaded)
    {
        public string? Version { get; set; }
        public string? TargetFramework { get; set; }
    }
}
