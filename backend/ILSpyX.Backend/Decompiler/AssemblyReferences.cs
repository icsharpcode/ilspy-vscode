// Copyright (c) 2021 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using ICSharpCode.Decompiler.Metadata;
using System;
using System.Text.RegularExpressions;

namespace ILSpyX.Backend.Decompiler
{
    static class AssemblyReferences
    {
        public static IAssemblyResolver CreateAssemblyResolver(string assemblyPath)
        {
            var assemblyDefinition = Mono.Cecil.AssemblyDefinition.ReadAssembly(assemblyPath);
            string tfi = DetectTargetFrameworkId(assemblyDefinition, assemblyPath);
            return new UniversalAssemblyResolver(assemblyPath, throwOnError: false, tfi);
        }

        static readonly string DetectTargetFrameworkIdRefPathPattern =
            @"(Reference Assemblies[/\\]Microsoft[/\\]Framework[/\\](?<1>.NETFramework)[/\\]v(?<2>[^/\\]+)[/\\])" +
            @"|((NuGetFallbackFolder|packs|.nuget[/\\]packages)[/\\](?<1>[^/\\]+)\\(?<2>[^/\\]+)([/\\].*)?[/\\]ref[/\\])";

        public static string DetectTargetFrameworkId(Mono.Cecil.AssemblyDefinition assembly, string assemblyPath)
        {
            if (assembly == null)
                throw new ArgumentNullException(nameof(assembly));

            const string TargetFrameworkAttributeName = "System.Runtime.Versioning.TargetFrameworkAttribute";

            foreach (var attribute in assembly.CustomAttributes)
            {
                if (attribute.AttributeType.FullName != TargetFrameworkAttributeName)
                    continue;
                if (attribute.HasConstructorArguments)
                {
                    if (attribute.ConstructorArguments[0].Value is string value)
                        return value;
                }
            }

            // Optionally try to detect target version through assembly path as a fallback (use case: reference assemblies)
            if (assemblyPath != null)
            {
                /*
				 * Detected path patterns (examples):
				 * 
				 * - .NETFramework -> C:\Program Files (x86)\Reference Assemblies\Microsoft\Framework\.NETFramework\v4.6.1\mscorlib.dll
				 * - .NETCore      -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\microsoft.netcore.app\2.1.0\ref\netcoreapp2.1\System.Console.dll
				 *                 -> C:\Program Files\dotnet\packs\Microsoft.NETCore.App.Ref\3.0.0\ref\netcoreapp3.0\System.Runtime.Extensions.dll
				 * - .NETStandard  -> C:\Program Files\dotnet\sdk\NuGetFallbackFolder\netstandard.library\2.0.3\build\netstandard2.0\ref\netstandard.dll
				 */
                var pathMatch = Regex.Match(assemblyPath, DetectTargetFrameworkIdRefPathPattern,
                    RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.ExplicitCapture);
                if (pathMatch.Success)
                {
                    var type = pathMatch.Groups[1].Value;
                    var version = pathMatch.Groups[2].Value;

                    if (type == ".NETFramework")
                    {
                        return $".NETFramework,Version=v{version}";
                    }
                    else if (type.ToLower().Contains("netcore"))
                    {
                        return $".NETCoreApp,Version=v{version}";
                    }
                    else if (type.ToLower().Contains("netstandard"))
                    {
                        return $".NETStandard,Version=v{version}";
                    }
                }
            }

            return string.Empty;
        }
    }
}
