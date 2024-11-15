// Copyright (c) Microsoft Corporation. All rights reserved.
// Copyright(c) 2017 ICSharpCode
// Licensed under the MIT license. See the LICENSE file in the project root for more information.

using System;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Text;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.CSharp.Syntax;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.TypeSystem;

namespace ILSpy.Backend.Decompiler
{
    public static class TypeSystemExtensions
    {
        static readonly CSharpFormattingOptions TypeToStringFormattingOptions = FormattingOptionsFactory.CreateEmpty();

        public static string TypeToString(this IType type, bool includeNamespace)
        {
            if (type == null)
                throw new ArgumentNullException(nameof(type));
            if (type is ITypeDefinition definition && definition.TypeParameterCount > 0)
            {
                return TypeToStringInternal(new ParameterizedType(definition, definition.TypeParameters), includeNamespace, false);
            }
            return TypeToStringInternal(type, includeNamespace, false);
        }

        static string TypeToStringInternal(IType t, bool includeNamespace, bool useBuiltinTypeNames = true,
            ReferenceKind referenceKind = ReferenceKind.None)
        {
            TypeSystemAstBuilder builder = new()
            {
                AlwaysUseShortTypeNames = !includeNamespace, UseKeywordsForBuiltinTypes = useBuiltinTypeNames
            };

            AstType astType = builder.ConvertType(t);
            if (referenceKind is ReferenceKind.Ref or ReferenceKind.Out or ReferenceKind.In &&
                astType is ComposedType { HasRefSpecifier: true } ct)
            {
                ct.HasRefSpecifier = false;
            }

            StringWriter w = new();

            astType.AcceptVisitor(new CSharpOutputVisitor(w, TypeToStringFormattingOptions));
            string output = w.ToString();

            return referenceKind switch
            {
                ReferenceKind.Ref => "ref " + output,
                ReferenceKind.Out => "out " + output,
                ReferenceKind.In => "in " + output,
                _ => output
            };
        }
        
        public static string FieldToString(this IField field, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            if (field == null)
                throw new ArgumentNullException(nameof(field));

            string simple = field.Name + " : " + field.Type.TypeToString(includeNamespace);
            if (!includeDeclaringTypeName)
                return simple;
            return TypeToStringInternal(field.DeclaringType, includeNamespaceOfDeclaringTypeName) + "." + simple;
        }

        public static string PropertyToString(this IProperty property, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            if (property == null)
                throw new ArgumentNullException(nameof(property));
            var buffer = new StringBuilder();
            if (includeDeclaringTypeName)
            {
                buffer.Append(property.DeclaringType.TypeToString(includeNamespaceOfDeclaringTypeName));
                buffer.Append('.');
            }
            if (property.IsIndexer)
            {
                if (property.IsExplicitInterfaceImplementation)
                {
                    string name = property.Name;
                    int index = name.LastIndexOf('.');
                    if (index > 0)
                    {
                        buffer.Append(name.Substring(0, index));
                        buffer.Append('.');
                    }
                }
                buffer.Append(@"this[");

                int i = 0;
                var parameters = property.Parameters;
                foreach (var param in parameters)
                {
                    if (i > 0)
                        buffer.Append(", ");
                    buffer.Append(TypeToStringInternal(param.Type, includeNamespace,
                        referenceKind: param.ReferenceKind));
                    i++;
                }

                buffer.Append(']');
            }
            else
            {
                buffer.Append(property.Name);
            }
            buffer.Append(" : ");
            buffer.Append(TypeToStringInternal(property.ReturnType, includeNamespace));
            return buffer.ToString();
        }

        public static string MethodToString(this IMethod method, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            if (method == null)
                throw new ArgumentNullException(nameof(method));
            string name;
            if (includeDeclaringTypeName)
            {
                name = method.DeclaringType.TypeToString(includeNamespace: includeNamespaceOfDeclaringTypeName) + ".";
            }
            else
            {
                name = "";
            }
            if (method.IsConstructor)
            {
                name += method.DeclaringType.TypeToString(false);
            }
            else
            {
                name += method.Name;
            }
            int i = 0;
            var buffer = new StringBuilder(name);

            if (method.TypeParameters.Count > 0)
            {
                buffer.Append('<');
                foreach (var tp in method.TypeParameters)
                {
                    if (i > 0)
                        buffer.Append(", ");
                    buffer.Append(tp.Name);
                    i++;
                }
                buffer.Append('>');
            }
            buffer.Append('(');

            i = 0;
            var parameters = method.Parameters;
            foreach (var param in parameters)
            {
                if (i > 0)
                    buffer.Append(", ");
                buffer.Append(
                    TypeToStringInternal(param.Type, includeNamespace, referenceKind: param.ReferenceKind));
                i++;
            }

            buffer.Append(')');
            if (!method.IsConstructor)
            {
                buffer.Append(" : ");
                buffer.Append(TypeToStringInternal(method.ReturnType, includeNamespace));
            }
            return buffer.ToString();
        }

        public static string EventToString(this IEvent @event, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
        {
            if (@event == null)
                throw new ArgumentNullException(nameof(@event));
            var buffer = new StringBuilder();
            if (includeDeclaringTypeName)
            {
                buffer.Append(@event.DeclaringType.TypeToString(includeNamespaceOfDeclaringTypeName) + ".");
            }
            buffer.Append(@event.Name);
            buffer.Append(" : ");
            buffer.Append(TypeToStringInternal(@event.ReturnType, includeNamespace));
            return buffer.ToString();
        }

        public static string GetPlatformDisplayName(this PEFile module)
        {
            var architecture = module.Reader.PEHeaders.CoffHeader.Machine;
            switch (architecture)
            {
                case Machine.I386:
                    var flags = module.Reader.PEHeaders.CorHeader?.Flags;
                    if ((flags & CorFlags.Prefers32Bit) != 0)
                    {
                        return "AnyCPU (32-bit preferred)";
                    }
                    else if ((flags & CorFlags.Requires32Bit) != 0)
                    {
                        return "x86";
                    }
                    else
                    {
                        return "AnyCPU (64-bit preferred)";
                    }
                case Machine.Amd64:
                    return "x64";
                case Machine.IA64:
                    return "Itanium";
                default:
                    return architecture.ToString();
            }
        }

        public static string GetRuntimeDisplayName(this PEFile module)
        {
            return module.Metadata.MetadataVersion;
        }
    }
}
