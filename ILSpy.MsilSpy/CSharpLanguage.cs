// Copyright (c) 2011 AlphaSierraPapa for the SharpDevelop Team
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy of this
// software and associated documentation files (the "Software"), to deal in the Software
// without restriction, including without limitation the rights to use, copy, modify, merge,
// publish, distribute, sublicense, and/or sell copies of the Software, and to permit persons
// to whom the Software is furnished to do so, subject to the following conditions:
// 
// The above copyright notice and this permission notice shall be included in all copies or
// substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR IMPLIED,
// INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, FITNESS FOR A PARTICULAR
// PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT HOLDERS BE LIABLE
// FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR
// OTHERWISE, ARISING FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER
// DEALINGS IN THE SOFTWARE.

// This is a modified version from https://github.com/icsharpcode/ILSpy/tree/v2.4/ILSpy/

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml;
using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.Ast;
using ICSharpCode.Decompiler.Ast.Transforms;
using ICSharpCode.NRefactory.CSharp;
using Mono.Cecil;
using MsilDecompiler.MsilSpy.XmlDoc;

namespace MsilDecompiler.MsilSpy
{
    public class CSharpLanguage : Language
    {
        const string name = "C#";
        public override string FileExtension => ".cs";

        public override string Name => name;

        public override string FormatTypeName(TypeDefinition type)
        {
            if (type == null)
                throw new ArgumentNullException("type");

            return TypeToString(ConvertTypeOptions.DoNotUsePrimitiveTypeNames | ConvertTypeOptions.IncludeTypeParameterDefinitions, type);
        }

        public override string FormatMethodName(MethodDefinition method)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));

            if (method.IsConstructor)
            {
                return method.IsStatic ? "cctor" : "ctor";
            }
            else
            {
                var fullname = method.FullName;
                var parts = fullname.Split(new[] { " ", "::" }, StringSplitOptions.None);

                return parts[2];
            }
        }

        public override string FormatPropertyName(PropertyDefinition property, bool? isIndexer = default(bool?))
        {
            if (property == null)
                throw new ArgumentNullException("property");

            if (!isIndexer.HasValue)
            {
                isIndexer = property.IsIndexer();
            }
            if (isIndexer.Value)
            {
                var buffer = new System.Text.StringBuilder();
                var accessor = property.GetMethod ?? property.SetMethod;
                if (accessor.HasOverrides)
                {
                    var declaringType = accessor.Overrides.First().DeclaringType;
                    buffer.Append(TypeToString(declaringType, includeNamespace: true));
                    buffer.Append(@".");
                }
                buffer.Append(@"this[");
                bool addSeparator = false;
                foreach (var p in property.Parameters)
                {
                    if (addSeparator)
                        buffer.Append(@", ");
                    else
                        addSeparator = true;
                    buffer.Append(TypeToString(p.ParameterType, includeNamespace: true));
                }
                buffer.Append(@"]");
                return buffer.ToString();
            }
            else
                return property.Name;
        }

        public override string TypeToString(TypeReference type, bool includeNamespace, ICustomAttributeProvider typeAttributes = null)
        {
            ConvertTypeOptions options = ConvertTypeOptions.IncludeTypeParameterDefinitions;
            if (includeNamespace)
                options |= ConvertTypeOptions.IncludeNamespace;

            return TypeToString(options, type, typeAttributes);
        }

        string TypeToString(ConvertTypeOptions options, TypeReference type, ICustomAttributeProvider typeAttributes = null)
        {
            AstType astType = AstBuilder.ConvertType(type, typeAttributes, options);

            StringWriter w = new StringWriter();
            if (type.IsByReference)
            {
                if (typeAttributes is ParameterDefinition pd && (!pd.IsIn && pd.IsOut))
                    w.Write("out ");
                else
                    w.Write("ref ");

                if (astType is ComposedType && ((ComposedType)astType).PointerRank > 0)
                    ((ComposedType)astType).PointerRank--;
            }

            astType.AcceptVisitor(new CSharpOutputVisitor(w, TypeToStringFormattingOptions));
            return w.ToString();
        }

        static readonly CSharpFormattingOptions TypeToStringFormattingOptions = FormattingOptionsFactory.CreateEmpty();

        public override void DecompileAssembly(AssemblyDefinition assemblyDefinition, ITextOutput output, DecompilerSettings settings)
        {
            base.DecompileAssembly(assemblyDefinition, output, settings);
            ModuleDefinition mainModule = assemblyDefinition.MainModule;
            if (mainModule.Types.Count > 0)
            {
                output.Write("// Global type: ");
                output.WriteReference(mainModule.Types[0].FullName, mainModule.Types[0]);
                output.WriteLine();
            }
            if (mainModule.EntryPoint != null)
            {
                output.Write("// Entry point: ");
                output.WriteReference(mainModule.EntryPoint.DeclaringType.FullName + "." + mainModule.EntryPoint.Name, mainModule.EntryPoint);
                output.WriteLine();
            }
            output.WriteLine("// Architecture: " + GetPlatformDisplayName(mainModule));
            if ((mainModule.Attributes & ModuleAttributes.ILOnly) == 0)
            {
                output.WriteLine("// This assembly contains unmanaged code.");
            }
            string runtimeName = GetRuntimeDisplayName(mainModule);
            if (runtimeName != null)
            {
                output.WriteLine("// Runtime: " + runtimeName);
            }
            output.WriteLine();

            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentModule: mainModule);
            codeDomBuilder.AddAssembly(mainModule, onlyAssemblyLevel: true);
            codeDomBuilder.GenerateCode(output);
        }

        public override void DecompileType(TypeDefinition type, ITextOutput output, DecompilerSettings settings)
        {
            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentModule: type.Module);
            codeDomBuilder.AddType(type);
            RunTransformsAndGenerateCode(codeDomBuilder, output, settings);
        }

        public override void DecompileMethod(MethodDefinition method, ITextOutput output, DecompilerSettings settings)
        {
            WriteCommentLine(output, $"Containing type: {TypeToString(method.DeclaringType, includeNamespace: true)}");
            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentType: method.DeclaringType, isSingleMember: true);
            if (method.IsConstructor && !method.IsStatic && !method.DeclaringType.IsValueType)
            {
                // also fields and other ctors so that the field initializers can be shown as such
                AddFieldsAndCtors(codeDomBuilder, method.DeclaringType, method.IsStatic);
                RunTransformsAndGenerateCode(codeDomBuilder, output, settings, new SelectCtorTransform(method));
            }
            else
            {
                codeDomBuilder.AddMethod(method);
                RunTransformsAndGenerateCode(codeDomBuilder, output, settings);
            }
        }

        public override void DecompileField(FieldDefinition field, ITextOutput output, DecompilerSettings settings)
        {
            WriteCommentLine(output, $"Containing type: {TypeToString(field.DeclaringType, includeNamespace: true)}");
            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentType: field.DeclaringType, isSingleMember: true);
            if (field.IsLiteral)
            {
                codeDomBuilder.AddField(field);
            }
            else
            {
                // also decompile ctors so that the field initializer can be shown
                AddFieldsAndCtors(codeDomBuilder, field.DeclaringType, field.IsStatic);
            }

            RunTransformsAndGenerateCode(codeDomBuilder, output, settings, new SelectFieldTransform(field));
        }

        public override void DecompileEvent(EventDefinition ev, ITextOutput output, DecompilerSettings settings)
        {
            WriteCommentLine(output, TypeToString(ev.DeclaringType, includeNamespace: true));
            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentType: ev.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddEvent(ev);
            RunTransformsAndGenerateCode(codeDomBuilder, output, settings);
        }

        public override void DecompileProperty(PropertyDefinition property, ITextOutput output, DecompilerSettings settings)
        {
            WriteCommentLine(output, TypeToString(property.DeclaringType, includeNamespace: true));
            AstBuilder codeDomBuilder = CreateAstBuilder(settings, currentType: property.DeclaringType, isSingleMember: true);
            codeDomBuilder.AddProperty(property);
            RunTransformsAndGenerateCode(codeDomBuilder, output, settings);
        }

        public static string GetPlatformDisplayName(ModuleDefinition module)
        {
            switch (module.Architecture)
            {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU (32-bit preferred)";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU (64-bit preferred)";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetPlatformName(ModuleDefinition module)
        {
            switch (module.Architecture)
            {
                case TargetArchitecture.I386:
                    if ((module.Attributes & ModuleAttributes.Preferred32Bit) == ModuleAttributes.Preferred32Bit)
                        return "AnyCPU";
                    else if ((module.Attributes & ModuleAttributes.Required32Bit) == ModuleAttributes.Required32Bit)
                        return "x86";
                    else
                        return "AnyCPU";
                case TargetArchitecture.AMD64:
                    return "x64";
                case TargetArchitecture.IA64:
                    return "Itanium";
                default:
                    return module.Architecture.ToString();
            }
        }

        public static string GetRuntimeDisplayName(ModuleDefinition module)
        {
            switch (module.Runtime)
            {
                case TargetRuntime.Net_1_0:
                    return ".NET 1.0";
                case TargetRuntime.Net_1_1:
                    return ".NET 1.1";
                case TargetRuntime.Net_2_0:
                    return ".NET 2.0";
                case TargetRuntime.Net_4_0:
                    return ".NET 4.0";
            }
            return null;
        }

        class SelectCtorTransform : IAstTransform
        {
            readonly MethodDefinition ctorDef;

            public SelectCtorTransform(MethodDefinition ctorDef)
            {
                this.ctorDef = ctorDef;
            }

            public void Run(AstNode compilationUnit)
            {
                ConstructorDeclaration ctorDecl = null;
                foreach (var node in compilationUnit.Children)
                {
                    if (node is ConstructorDeclaration ctor)
                    {
                        if (ctor.Annotation<MethodDefinition>() == ctorDef)
                        {
                            ctorDecl = ctor;
                        }
                        else
                        {
                            // remove other ctors
                            ctor.Remove();
                        }
                    }
                    // Remove any fields without initializers
                    if (node is FieldDeclaration fd && fd.Variables.All(v => v.Initializer.IsNull))
                        fd.Remove();
                }
                if (ctorDecl.Initializer.ConstructorInitializerType == ConstructorInitializerType.This)
                {
                    // remove all fields
                    foreach (var node in compilationUnit.Children)
                        if (node is FieldDeclaration)
                            node.Remove();
                }
            }
        }

        /// <summary>
        /// Removes all top-level members except for the specified fields.
        /// </summary>
        sealed class SelectFieldTransform : IAstTransform
        {
            readonly FieldDefinition field;

            public SelectFieldTransform(FieldDefinition field)
            {
                this.field = field;
            }

            public void Run(AstNode compilationUnit)
            {
                foreach (var child in compilationUnit.Children)
                {
                    if (child is EntityDeclaration)
                    {
                        if (child.Annotation<FieldDefinition>() != field)
                            child.Remove();
                    }
                }
            }
        }
        AstBuilder CreateAstBuilder(DecompilerSettings settings, ModuleDefinition currentModule = null, TypeDefinition currentType = null, bool isSingleMember = false)
        {
            if (currentModule == null)
                currentModule = currentType.Module;
            if (isSingleMember)
            {
                settings = settings.Clone();
                settings.UsingDeclarations = false;
            }
            return new AstBuilder(
                new DecompilerContext(currentModule)
                {
                    CancellationToken = CancellationToken.None,
                    CurrentType = currentType,
                    Settings = settings
                });
        }

        void AddFieldsAndCtors(AstBuilder codeDomBuilder, TypeDefinition declaringType, bool isStatic)
        {
            foreach (var field in declaringType.Fields)
            {
                if (field.IsStatic == isStatic)
                    codeDomBuilder.AddField(field);
            }
            foreach (var ctor in declaringType.Methods)
            {
                if (ctor.IsConstructor && ctor.IsStatic == isStatic)
                    codeDomBuilder.AddMethod(ctor);
            }
        }

        void RunTransformsAndGenerateCode(AstBuilder astBuilder, ITextOutput output, DecompilerSettings settings, IAstTransform additionalTransform = null)
        {
            if (additionalTransform != null)
            {
                additionalTransform.Run(astBuilder.SyntaxTree);
            }
            if (settings.ShowXmlDocumentation)
            {
                try
                {
                    AddXmlDocTransform.Run(astBuilder.SyntaxTree);
                }
                catch (XmlException ex)
                {
                    string[] msg = (" Exception while reading XmlDoc: " + ex.ToString()).Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                    var insertionPoint = astBuilder.SyntaxTree.FirstChild;
                    for (int i = 0; i < msg.Length; i++)
                        astBuilder.SyntaxTree.InsertChildBefore(insertionPoint, new Comment(msg[i], CommentType.Documentation), Roles.Comment);
                }
            }
            astBuilder.GenerateCode(output);
        }

    }
}
