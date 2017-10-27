using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using ICSharpCode.Decompiler.Extensions;
using ICSharpCode.Decompiler.TypeSystem;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using OmniSharp.Host.Services;

namespace ILSpy.Host.Providers
{
    public class SimpleDecompilationProvider : IDecompilationProvider
    {
        private ILogger _logger;
        private Dictionary<ModuleDefinition, Dictionary<MetadataToken, IEntity>> _tokenToProviderMap = new Dictionary<ModuleDefinition, Dictionary<MetadataToken, IEntity>>();
        private Dictionary<string, ModuleDefinition> _moduleDefinitions = new Dictionary<string, ModuleDefinition>();
        private Dictionary<ModuleDefinition, SimpleDecompiler> _decompilers = new Dictionary<ModuleDefinition, SimpleDecompiler>();

        public SimpleDecompilationProvider(IMsilDecompilerEnvironment decompilationConfiguration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<SimpleDecompilationProvider>();
        }

        public bool AddAssembly(string path)
        {
            try
            {
                var asm = SimpleAssemblyLoader.LoadModule(path);
                if (asm != null)
                {
                    _moduleDefinitions[path] = asm;
                    PopulateTokenToProviderMap(asm);
                    return true;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError("An exception occurred when reading assembly {assembly}: {exception}", path, ex);
            }

            return false;
        }

        private void PopulateTokenToProviderMap(ModuleDefinition module)
        {
            var dc = SimpleDecompiler.Create(module);
            _decompilers[module] = dc;
            var tds = dc.ListContent(new HashSet<TypeKind>() { TypeKind.Class, TypeKind.Interface, TypeKind.Enum, TypeKind.Delegate, TypeKind.Struct });
            foreach (var type in tds)
            {
                PopulateTokenToProviderMap(dc, module, type);
            }
        }

        private void PopulateTokenToProviderMap(SimpleDecompiler dc, ModuleDefinition module, ITypeDefinition typeDefinition)
        {
            if (typeDefinition == null)
            {
                return;
            }

            AddToProviderMap(dc, module, typeDefinition);

            foreach (var member in typeDefinition.Members)
            {
                AddToProviderMap(dc, module, member);
            }

            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                PopulateTokenToProviderMap(dc, module, nestedType);
            }
        }

        private void AddToProviderMap(SimpleDecompiler dc, ModuleDefinition module, IEntity entity)
        {
            if (!_tokenToProviderMap.ContainsKey(module))
            {
                _tokenToProviderMap.Add(module, new Dictionary<MetadataToken, IEntity>());
            }

            var token = entity is ITypeDefinition type
                ? dc.TypeSystem.GetCecil(type)?.MetadataToken
                : dc.TypeSystem.GetCecil((IMember)entity)?.MetadataToken;
            if (token.HasValue)
            {
                _tokenToProviderMap[module][token.Value] = entity;
            }
        }

        public IEnumerable<MemberData> GetChildren(string assemblyPath, TokenType tokenType, uint rid)
        {
            var module = _moduleDefinitions[assemblyPath];
            var dc = _decompilers[module];
            var c = _tokenToProviderMap[module][new MetadataToken(tokenType, rid)] as ITypeDefinition;

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
                    }
                    ).Union(
                c.Members.Select(m =>
                {
                    var cecilRef = dc.TypeSystem.GetCecil(m);
                    return new MemberData
                    {
                        Name = m.Name,
                        Token = cecilRef.MetadataToken,
                        MemberSubKind = MemberSubKind.None
                    };
                }));
        }

        public string GetCode(string assemblyPath, TokenType tokenType, uint rid)
        {
            var module = _moduleDefinitions[assemblyPath];
            if (tokenType == TokenType.Assembly)
            {
                return GetAssemblyCode(assemblyPath, module);
            }
            else if (tokenType == TokenType.TypeDef)
            {
                var c = _tokenToProviderMap[module][new MetadataToken(tokenType, rid)];
                return GetEntityCode(module, c);
            }

            return string.Empty;
        }

        private static string GetAssemblyCode(string assemblyPath, ModuleDefinition module)
        {
            using (var output = new StringWriter())
            {
                WriteCommentLine(output, assemblyPath);
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
            var module = _moduleDefinitions[assemblyPath];
            var entity = _tokenToProviderMap[module][memberToken];
            return GetEntityCode(module, entity);
        }

        private string GetEntityCode(ModuleDefinition module, IEntity entity)
        {
            if (entity != null)
            {
                var dc = _decompilers[module];
                var sw = new StringWriter();
                if (entity is ITypeDefinition type)
                {
                    dc.Decompile(sw, type.FullName);
                }
                else if (entity is IMember member)
                {
                    dc.Decompile(sw, member);
                }

                return sw.ToString();
            }

            return string.Empty;
        }

        public IEnumerable<MemberData> ListTypes(string assemblyPath)
        {
            var module = _moduleDefinitions[assemblyPath];

            var dc = _decompilers[module];
            var tds = dc.ListContent(new HashSet<TypeKind>() { TypeKind.Class, TypeKind.Interface, TypeKind.Enum, TypeKind.Delegate, TypeKind.Struct });

            foreach (var t in tds)
            {
                var cecilType = dc.TypeSystem.GetCecil(t);
                yield return new MemberData
                {
                    Name = t.Name,
                    Token = cecilType.MetadataToken,
                    MemberSubKind = cecilType.GetMemberSubKind()
                };
            }
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
