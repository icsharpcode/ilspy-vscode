using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using OmniSharp.Host.Services;

namespace ILSpy.Host.Providers
{
    public class SimpleDecompilationProvider : IDecompilationProvider
    {
        private ILogger _logger;
        private Dictionary<string, Dictionary<MetadataToken, IEntity>> _tokenToProviderMap = new Dictionary<string, Dictionary<MetadataToken, IEntity>>();
        private Dictionary<string, CSharpDecompiler> _decompilers = new Dictionary<string, CSharpDecompiler>();
        private Dictionary<string, ModuleDefinition> _mainModules = new Dictionary<string, ModuleDefinition>();

        public SimpleDecompilationProvider(IMsilDecompilerEnvironment decompilationConfiguration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleDecompilationProvider>();
        }

        public bool AddAssembly(string path)
        {
            try
            {
                var mainModule = UniversalAssemblyResolver.LoadMainModule(path);
                if (mainModule != null)
                {
                    _mainModules.Add(path, mainModule);
                    PopulateTokenToProviderMap(path);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("An exception occurred when reading assembly {assembly}: {exception}", path, ex);
            }

            return false;
        }

        private void PopulateTokenToProviderMap(string assemblyPath)
        {
            var decompiler = new CSharpDecompiler(assemblyPath, new DecompilerSettings());
            _decompilers[assemblyPath] = decompiler;
            var types = decompiler.TypeSystem.Compilation.MainAssembly.GetAllTypeDefinitions();
            foreach (var type in types)
            {
                PopulateTokenToProviderMap(decompiler, assemblyPath, type);
            }
        }

        private void PopulateTokenToProviderMap(CSharpDecompiler dc, string assemblyPath, ITypeDefinition typeDefinition)
        {
            if (typeDefinition == null)
            {
                return;
            }

            AddToProviderMap(dc, assemblyPath, typeDefinition);

            foreach (var member in typeDefinition.Members)
            {
                AddToProviderMap(dc, assemblyPath, member);
            }

            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                PopulateTokenToProviderMap(dc, assemblyPath, nestedType);
            }
        }

        private void AddToProviderMap(CSharpDecompiler dc, string assemblyPath, IEntity entity)
        {
            if (!_tokenToProviderMap.ContainsKey(assemblyPath))
            {
                _tokenToProviderMap.Add(assemblyPath, new Dictionary<MetadataToken, IEntity>());
            }

            var token = entity is ITypeDefinition type
                ? dc.TypeSystem.GetCecil(type)?.MetadataToken
                : dc.TypeSystem.GetCecil((IMember)entity)?.MetadataToken;
            if (token.HasValue)
            {
                _tokenToProviderMap[assemblyPath][token.Value] = entity;
            }
        }

        public IEnumerable<MemberData> GetChildren(string assemblyPath, TokenType tokenType, uint rid)
        {
            var dc = _decompilers[assemblyPath];
            var c = _tokenToProviderMap[assemblyPath][new MetadataToken(tokenType, rid)] as ITypeDefinition;

            return c == null
                ? new List<MemberData>()
                : c.NestedTypes.Select(typeDefinition =>
                    {
                        var cecilType = dc.TypeSystem.GetCecil(typeDefinition);
                        return new MemberData
                        {
                            Name = typeDefinition.Name,
                            Token = cecilType.MetadataToken,
                            MemberSubKind = cecilType.GetMemberSubKind()
                        };
                    })
                    .Union(c.Fields.Select(GetMemberData))
                    .Union(c.Properties.Select(GetMemberData))
                    .Union(c.Events.Select(GetMemberData))
                    .Union(c.Methods.Select(GetMemberData));

            MemberData GetMemberData(IMember member)
            {
                var cecilRef = dc.TypeSystem.GetCecil(member);
                var memberName = member is IMethod
                    ? ((MethodDefinition)cecilRef.Resolve()).GetFormattedText()
                    : member.Name;
                return new MemberData
                {
                    Name = memberName,
                    Token = cecilRef.MetadataToken,
                    MemberSubKind = MemberSubKind.None
                };
            }
        }


        public string GetCode(string assemblyPath, TokenType tokenType, uint rid)
        {
            if (tokenType == TokenType.Assembly)
            {
                return GetAssemblyCode(assemblyPath);
            }
            else if (tokenType == TokenType.TypeDef)
            {
                var c = _tokenToProviderMap[assemblyPath][new MetadataToken(tokenType, rid)];
                return GetEntityCode(assemblyPath, c);
            }

            return string.Empty;
        }

        private string GetAssemblyCode(string assemblyPath)
        {
            using (var output = new StringWriter())
            {
                WriteCommentLine(output, assemblyPath);
                var decompiler = _decompilers[assemblyPath];
                var module = _mainModules[assemblyPath];
                var assembly = module.Assembly;
                if (assembly != null)
                {
                    var name = assembly.Name;
                    if (name.IsWindowsRuntime)
                    {
                        WriteCommentLine(output, name.Name + " [WinRT]");
                    }
                    else
                    {
                        WriteCommentLine(output, name.FullName);
                    }
                }
                else
                {
                    WriteCommentLine(output, module.Name);
                }

                if (module.Types.Count > 0)
                {
                    output.Write("// Global type: ");
                    output.WriteReference(module.Types[0].FullName, module.Types[0]);
                    output.WriteLine();
                }
                if (module.EntryPoint != null)
                {
                    output.Write("// Entry point: ");
                    output.WriteReference(module.EntryPoint.DeclaringType.FullName + "." + module.EntryPoint.Name, module.EntryPoint);
                    output.WriteLine();
                }
                output.WriteLine("// Architecture: " + module.GetPlatformDisplayName());
                if ((module.Attributes & ModuleAttributes.ILOnly) == 0)
                {
                    output.WriteLine("// This assembly contains unmanaged code.");
                }
                string runtimeName = module.GetRuntimeDisplayName();
                if (runtimeName != null)
                {
                    output.WriteLine("// Runtime: " + runtimeName);
                }

                output.Write(decompiler.DecompileModuleAndAssemblyAttributesToString());

                output.WriteLine();

                return output.ToString();
            }
        }

        private static void WriteCommentLine(StringWriter output, string s)
        {
            output.WriteLine($"// {s}");
        }

        public string GetMemberCode(string assemblyPath, MetadataToken memberToken)
        {
            var entity = _tokenToProviderMap[assemblyPath][memberToken];
            return GetEntityCode(assemblyPath, entity);
        }

        private string GetEntityCode(string assemblyPath, IEntity entity)
        {
            if (entity != null)
            {
                var dc = _decompilers[assemblyPath];
                if (entity is ITypeDefinition type)
                {
                    var cecilType = dc.TypeSystem.GetCecil(type);
                    return dc.DecompileTypesAsString(new[] { cecilType });
                }
                else if (entity is IMember member)
                {
                    var memberDef = dc.TypeSystem.GetCecil(member).Resolve();
                    return dc.DecompileAsString(memberDef);
                }
            }

            return string.Empty;
        }

        public IEnumerable<MemberData> ListTypes(string assemblyPath, string @namespace)
        {
            var decompiler = _decompilers[assemblyPath];
            var cecilTypes = decompiler.TypeSystem.Compilation.MainAssembly.GetAllTypeDefinitions()
                .Select(t => decompiler.TypeSystem.GetCecil(t))
                .Where(t => !t.IsNested)
                .Where(t => t.Namespace.Equals(@namespace, StringComparison.Ordinal));

            foreach (var t in cecilTypes)
            {
                yield return new MemberData
                {
                    Name = t.Name,
                    Token = t.MetadataToken,
                    MemberSubKind = t.GetMemberSubKind()
                };
            }
        }

        public IEnumerable<string> ListNamespaces(string assemblyPath)
        {
            var decompiler = _decompilers[assemblyPath];
            var types = decompiler.TypeSystem.Compilation.MainAssembly.GetAllTypeDefinitions();
            var namespaces = types.Select(t =>
            {
                var cecilType = decompiler.TypeSystem.GetCecil(t);
                var ns = t.Namespace;
                return ns;
            })
            .Distinct()
            .OrderBy(n => n);

            return namespaces;
        }
    }

    static class Extensions
    {
        public static void WriteReference(this TextWriter writer, string text, object reference, bool isLocal = false)
        {
            writer.Write(text);
        }
    }
}
