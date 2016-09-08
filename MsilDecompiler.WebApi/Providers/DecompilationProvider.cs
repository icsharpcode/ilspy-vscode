using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using MsilDecompiler.MsilSpy;
using MsilDecompiler.WebApi.Configuration;

namespace MsilDecompiler.WebApi.Providers
{
    public class DecompilationProvider : IDecompilationProvider
    {
        private ILogger _logger;
        private Dictionary<string, MetadataToken> _memberCache;
        private Dictionary<MetadataToken, IMetadataTokenProvider> _tokenToProviderMap;
        private AssemblyDefinition _assemblyDefinition;
        private IDecompilationConfiguration _decompilationConfiguration;
        private CSharpLanguage _csharpLanuage;

        public bool IsDotNetAssembly { get; private set; }

        public DecompilationProvider(IDecompilationConfiguration decompilationConfiguration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DecompilationProvider>();
            _decompilationConfiguration = decompilationConfiguration;
            _csharpLanuage = new CSharpLanguage();

            Initialize();
        }

        private void Initialize()
        {
            if (_assemblyDefinition == null)
            {
                try
                {
                    _assemblyDefinition = AssemblyDefinition.ReadAssembly(_decompilationConfiguration.FilePath, new ReaderParameters { AssemblyResolver = new MyDefaultAssemblyResolver() });
                    if (_assemblyDefinition != null)
                    {
                        IsDotNetAssembly = true;
                        _tokenToProviderMap = new Dictionary<MetadataToken, IMetadataTokenProvider>();
                    }

                    PopulateTokenToProviderMap(_assemblyDefinition);
                }
                catch (Exception ex)
                {
                    _logger.LogError("An exception occurred when reading assembly {assembly}: {exception}", _decompilationConfiguration.FilePath, ex);
                }
            }
        }

        public void PopulateTokenToProviderMap(AssemblyDefinition assemblyDefinition)
        {
            _tokenToProviderMap.Add(assemblyDefinition.MetadataToken, assemblyDefinition);
            foreach(var moduleDefinition in _assemblyDefinition.Modules)
            {
                foreach(var typeDefinition in moduleDefinition.Types)
                {
                    PopulateTokenToProviderMap(typeDefinition);
                }
            }
        }

        private void PopulateTokenToProviderMap(TypeDefinition typeDefinition)
        {
            if (typeDefinition == null)
            {
                return;
            }

            _tokenToProviderMap.Add(typeDefinition.MetadataToken, typeDefinition);

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                _tokenToProviderMap.Add(methodDefinition.MetadataToken, methodDefinition);
            }

            foreach (var eventDefinition in typeDefinition.Events)
            {
                _tokenToProviderMap.Add(eventDefinition.MetadataToken, eventDefinition);
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                _tokenToProviderMap.Add(fieldDefinition.MetadataToken, fieldDefinition);
            }

            foreach (var propertyDefinition in typeDefinition.Properties)
            {
                _tokenToProviderMap.Add(propertyDefinition.MetadataToken, propertyDefinition);
            }

            foreach (var nestedType in typeDefinition.NestedTypes)
            {
                PopulateTokenToProviderMap(nestedType);
            }
        }

        private IEnumerable<ModuleDefinition> GetModules()
        {
            foreach (var moduleDefinition in _assemblyDefinition.Modules)
            {
                yield return moduleDefinition;
            }
        }

        private IEnumerable<TypeDefinition> GetTypes()
        {
            foreach (var moduleDefinition in GetModules())
            {
                foreach (var typeDefinition in moduleDefinition.Types)
                {

                    yield return typeDefinition;
                }
            }
        }

        private TypeDefinition GetType(string fullTypeName)
        {
            var type = GetTypes().SingleOrDefault(t => t.FullName == fullTypeName);
            return type;
        }

        private IEnumerable<MethodDefinition> GetMethods(string fullTypename)
        {
            var typeDefinition = GetType(fullTypename);
            if (typeDefinition != null && typeDefinition.HasMethods)
            {
                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    yield return methodDefinition;
                }
            }
        }

        private string GetCSharpCode<T>(T t)
        {
            using (StringWriter writer = new StringWriter())
            {
                var output = new PlainTextOutput(writer);
                var settings = _decompilationConfiguration.DecompilerSettings;
                if (t is AssemblyDefinition)
                {
                    _csharpLanuage.DecompileAssembly(t as AssemblyDefinition, output, settings);
                }
                else if (t is TypeDefinition)
                {
                    _csharpLanuage.DecompileType(t as TypeDefinition, output, settings);
                }
                else if (t is MethodDefinition)
                {
                    _csharpLanuage.DecompileMethod(t as MethodDefinition, output, settings);
                }
                else if (t is FieldDefinition)
                {
                    _csharpLanuage.DecompileField(t as FieldDefinition, output, settings);
                }
                else if (t is EventDefinition)
                {
                    _csharpLanuage.DecompileEvent(t as EventDefinition, output, settings);
                }
                else if (t is PropertyDefinition)
                {
                    _csharpLanuage.DecompileProperty(t as PropertyDefinition, output, settings);
                }

                return writer.ToString();
            }
        }

        public IEnumerable<Tuple<string, MetadataToken>> GetTypeTuples()
        {
            return GetTypes().Select(t => Tuple.Create(_csharpLanuage.FormatTypeName(t), t.MetadataToken));
        }

        public string GetCode(TokenType type, uint rid)
        {
            var provider = _tokenToProviderMap[new MetadataToken(type, rid)];
            return GetCSharpCode(provider);
        }

        public IEnumerable<Tuple<string, MetadataToken>> GetChildren(TokenType type, uint rid)
        {
            var typeDefinition = _tokenToProviderMap[new MetadataToken(type, rid)] as TypeDefinition;
            if (typeDefinition != null)
            {
                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    yield return Tuple.Create(_csharpLanuage.FormatMethodName(methodDefinition), methodDefinition.MetadataToken);
                }

                foreach (var eventDefinition in typeDefinition.Events)
                {
                    _tokenToProviderMap.Add(eventDefinition.MetadataToken, eventDefinition);
                }

                foreach (var fieldDefinition in typeDefinition.Fields)
                {
                    _tokenToProviderMap.Add(fieldDefinition.MetadataToken, fieldDefinition);
                }

                foreach (var propertyDefinition in typeDefinition.Properties)
                {
                    _tokenToProviderMap.Add(propertyDefinition.MetadataToken, propertyDefinition);
                }

                foreach (var nestedType in typeDefinition.NestedTypes)
                {
                    PopulateTokenToProviderMap(nestedType);
                }
            }
        }

        public string GetMethodText(MethodDefinition methodDefinition)
        {
            return _csharpLanuage.FormatMethodName(methodDefinition);
        }

        private IEnumerable<TypeDefinition> GetNestedTypes(TypeDefinition typeDefinition)
        {
            if (typeDefinition != null && typeDefinition.HasNestedTypes)
            {
                foreach (var type in typeDefinition.NestedTypes)
                {
                    yield return type;
                }
            }
        }

        private class MyDefaultAssemblyResolver : DefaultAssemblyResolver
        {
            public override AssemblyDefinition Resolve(AssemblyNameReference name)
            {
                try
                {
                    return base.Resolve(name);
                }
                catch { }
                return null;
            }

            public override AssemblyDefinition Resolve(AssemblyNameReference name, ReaderParameters parameters)
            {
                try
                {
                    return base.Resolve(name, parameters);
                }
                catch { }
                return null;
            }

            public override AssemblyDefinition Resolve(string fullName)
            {
                try
                {
                    return base.Resolve(fullName);
                }
                catch { }
                return null;
            }

            public override AssemblyDefinition Resolve(string fullName, ReaderParameters parameters)
            {
                try
                {
                    return base.Resolve(fullName, parameters);
                }
                catch { }
                return null;
            }
        }

    }
}
