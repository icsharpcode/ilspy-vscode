// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

namespace ILSpyX.Backend.Model;

public enum PackageType
{
    None,
    NuGet,
    Other
}

public record AssemblyData()
{
    public required string Name { get; init; }
    public required string FilePath { get; init; }
    public required bool IsAutoLoaded { get; init; }
    public string? Version { get; init; }
    public string? TargetFramework { get; init; }
    public string? ParentBundleFilePath { get; init; }
    public PackageType PackageType { get; init; } = PackageType.None;
}