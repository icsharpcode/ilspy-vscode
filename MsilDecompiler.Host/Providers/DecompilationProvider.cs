using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ICSharpCode.Decompiler;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using MsilDecompiler.MsilSpy;
using MsilDecompiler.Host.Configuration;
using OmniSharp.Host.Services;

namespace MsilDecompiler.Host.Providers
{
    public class DecompilationProvider : IDecompilationProvider
    {
        private ILogger _logger;
        private Dictionary<MetadataToken, IMetadataTokenProvider> _tokenToProviderMap;
        private AssemblyDefinition _assemblyDefinition;
        private IMsilDecompilerEnvironment _decompilationConfiguration;

        //TODO: change to a list of supported languages
        private Language _language;

        public bool IsDotNetAssembly { get; private set; }

        public DecompilationProvider(IMsilDecompilerEnvironment decompilationConfiguration, ILoggerFactory loggerFactory)
        {
            _logger = loggerFactory.CreateLogger<DecompilationProvider>();
            _decompilationConfiguration = decompilationConfiguration;
            _language = new CSharpLanguage();

            Initialize();
        }

        private void Initialize()
        {
            if (_assemblyDefinition == null)
            {
                try
                {
                    _assemblyDefinition = AssemblyDefinition.ReadAssembly(
                        _decompilationConfiguration.AssemblyPath,
                        new ReaderParameters { AssemblyResolver = new MyDefaultAssemblyResolver() });
                    if (_assemblyDefinition != null)
                    {
                        IsDotNetAssembly = true;
                        _tokenToProviderMap = new Dictionary<MetadataToken, IMetadataTokenProvider>();
                    }

                    PopulateTokenToProviderMap(_assemblyDefinition);
                }
                catch (Exception ex)
                {
                    _logger.LogError("An exception occurred when reading assembly {assembly}: {exception}", _decompilationConfiguration.AssemblyPath, ex);
                }
            }
        }

        public void PopulateTokenToProviderMap(AssemblyDefinition assemblyDefinition)
        {
            TryAddToProviderMap(assemblyDefinition);
            foreach (var moduleDefinition in _assemblyDefinition.Modules)
            {
                foreach (var typeDefinition in moduleDefinition.Types)
                {
                    PopulateTokenToProviderMap(typeDefinition);
                }
            }
        }

        private void TryAddToProviderMap(IMetadataTokenProvider provider)
        {
            if (!_tokenToProviderMap.ContainsKey(provider.MetadataToken))
                _tokenToProviderMap.Add(provider.MetadataToken, provider);
        }

        private void PopulateTokenToProviderMap(TypeDefinition typeDefinition)
        {
            if (typeDefinition == null)
            {
                return;
            }

            TryAddToProviderMap(typeDefinition);

            foreach (var methodDefinition in typeDefinition.Methods)
            {
                TryAddToProviderMap(methodDefinition);
            }

            foreach (var eventDefinition in typeDefinition.Events)
            {
                TryAddToProviderMap(eventDefinition);
            }

            foreach (var fieldDefinition in typeDefinition.Fields)
            {
                TryAddToProviderMap(fieldDefinition);
            }

            foreach (var propertyDefinition in typeDefinition.Properties)
            {
                TryAddToProviderMap(propertyDefinition);
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
                    _language.DecompileAssembly(t as AssemblyDefinition, output, settings);
                }
                else if (t is TypeDefinition)
                {
                    _language.DecompileType(t as TypeDefinition, output, settings);
                }
                else if (t is MethodDefinition)
                {
                    _language.DecompileMethod(t as MethodDefinition, output, settings);
                }
                else if (t is FieldDefinition)
                {
                    _language.DecompileField(t as FieldDefinition, output, settings);
                }
                else if (t is EventDefinition)
                {
                    _language.DecompileEvent(t as EventDefinition, output, settings);
                }
                else if (t is PropertyDefinition)
                {
                    _language.DecompileProperty(t as PropertyDefinition, output, settings);
                }

                return writer.ToString();
            }
        }

        public IEnumerable<Tuple<string, MetadataToken>> GetTypeTuples()
        {
            return GetTypes().Select(t => Tuple.Create(_language.FormatTypeName(t), t.MetadataToken));
        }

        public string GetCode(TokenType type, uint rid)
        {
            if (rid == 0)
            {
                return GetCSharpCode(_assemblyDefinition);
            }

            var provider = _tokenToProviderMap[new MetadataToken(type, rid)];
            return GetCSharpCode(provider);
        }

        public string GetMemberCode(MetadataToken token)
        {
            var tokenType = token.TokenType;
            if (tokenType != TokenType.Event &&
                tokenType != TokenType.Field &&
                tokenType != TokenType.Property &&
                tokenType != TokenType.Method &&
                tokenType != TokenType.TypeDef)
            {
                return string.Empty;
            }

            var provider = _tokenToProviderMap[token];
            return GetCSharpCode(provider);
        }

        public IEnumerable<Tuple<string, MetadataToken>> GetChildren(TokenType type, uint rid)
        {
            var typeDefinition = _tokenToProviderMap[new MetadataToken(type, rid)] as TypeDefinition;
            if (typeDefinition != null)
            {
                foreach (var methodDefinition in typeDefinition.Methods)
                {
                    yield return Tuple.Create(_language.FormatMethodName(methodDefinition), methodDefinition.MetadataToken);
                }

                foreach (var eventDefinition in typeDefinition.Events)
                {
                    yield return Tuple.Create(_language.FormatEventName(eventDefinition), eventDefinition.MetadataToken);
                }

                foreach (var fieldDefinition in typeDefinition.Fields)
                {
                    yield return Tuple.Create(_language.FormatFieldName(fieldDefinition), fieldDefinition.MetadataToken);
                }

                foreach (var propertyDefinition in typeDefinition.Properties)
                {
                    yield return Tuple.Create(_language.FormatPropertyName(propertyDefinition), propertyDefinition.MetadataToken);
                }

                foreach (var nestedType in typeDefinition.NestedTypes)
                {
                    yield return Tuple.Create(_language.FormatTypeName(nestedType), nestedType.MetadataToken);
                }
            }
        }

        public string GetMethodText(MethodDefinition methodDefinition)
        {
            return _language.FormatMethodName(methodDefinition);
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
