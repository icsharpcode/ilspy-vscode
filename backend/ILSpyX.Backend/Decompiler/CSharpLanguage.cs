using ICSharpCode.Decompiler;
using ICSharpCode.Decompiler.CSharp;
using ICSharpCode.Decompiler.CSharp.OutputVisitor;
using ICSharpCode.Decompiler.Metadata;
using ICSharpCode.Decompiler.Output;
using ICSharpCode.Decompiler.TypeSystem;
using ICSharpCode.ILSpyX.Abstractions;
using System;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Text;

namespace ILSpy.Backend.Decompiler;

public class CSharpLanguage : ILanguage
{
    public CSharpLanguage()
    {
    }

    public string EventToString(IEvent @event, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
    {
        if (@event == null)
        {
            throw new ArgumentNullException(nameof(@event));
        }
        return EntityToString(@event, includeDeclaringTypeName, includeNamespace, includeNamespaceOfDeclaringTypeName);
    }

    public string FieldToString(IField field, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
    {
        if (field == null)
        {
            throw new ArgumentNullException(nameof(field));
        }
        return EntityToString(field, includeDeclaringTypeName, includeNamespace, includeNamespaceOfDeclaringTypeName);
    }

    public string GetEntityName(MetadataFile module, EntityHandle handle, bool fullName, bool omitGenerics)
    {
        var metadata = module.Metadata;
        switch (handle.Kind)
        {
            case HandleKind.TypeDefinition:
                return ToCSharpString(metadata, (TypeDefinitionHandle) handle, fullName, omitGenerics);
            case HandleKind.FieldDefinition:
                var fd = metadata.GetFieldDefinition((FieldDefinitionHandle) handle);
                var declaringType = fd.GetDeclaringType();
                if (fullName)
                {
                    return ToCSharpString(metadata, declaringType, fullName, omitGenerics) + "." + metadata.GetString(fd.Name);
                }
                return metadata.GetString(fd.Name);
            case HandleKind.MethodDefinition:
                var md = metadata.GetMethodDefinition((MethodDefinitionHandle) handle);
                declaringType = md.GetDeclaringType();
                string methodName = metadata.GetString(md.Name);
                switch (methodName)
                {
                    case ".ctor":
                    case ".cctor":
                        var td = metadata.GetTypeDefinition(declaringType);
                        methodName = ReflectionHelper.SplitTypeParameterCountFromReflectionName(metadata.GetString(td.Name));
                        break;
                    case "Finalize":
                        const MethodAttributes finalizerAttributes = (MethodAttributes.Virtual | MethodAttributes.Family | MethodAttributes.HideBySig);
                        if ((md.Attributes & finalizerAttributes) != finalizerAttributes)
                        {
                            goto default;
                        }
                        var methodSignature = md.DecodeSignature(MetadataExtensions.MinimalSignatureTypeProvider, default);
                        if (methodSignature.GenericParameterCount != 0 || methodSignature.ParameterTypes.Length != 0)
                        {
                            goto default;
                        }
                        td = metadata.GetTypeDefinition(declaringType);
                        methodName = "~" + ReflectionHelper.SplitTypeParameterCountFromReflectionName(metadata.GetString(td.Name));
                        break;
                    default:
                        var genericParams = md.GetGenericParameters();
                        if (!omitGenerics && genericParams.Count > 0)
                        {
                            methodName += "<";
                            int i = 0;
                            foreach (var h in genericParams)
                            {
                                if (i > 0)
                                {
                                    methodName += ",";
                                }
                                var gp = metadata.GetGenericParameter(h);
                                methodName += metadata.GetString(gp.Name);
                            }
                            methodName += ">";
                        }
                        break;
                }
                if (fullName)
                {
                    return ToCSharpString(metadata, declaringType, fullName, omitGenerics) + "." + methodName;
                }
                return methodName;
            case HandleKind.EventDefinition:
                var ed = metadata.GetEventDefinition((EventDefinitionHandle) handle);
                declaringType = metadata.GetMethodDefinition(ed.GetAccessors().GetAny()).GetDeclaringType();
                if (fullName && !declaringType.IsNil)
                {
                    return ToCSharpString(metadata, declaringType, fullName, omitGenerics) + "." + metadata.GetString(ed.Name);
                }
                return metadata.GetString(ed.Name);
            case HandleKind.PropertyDefinition:
                var pd = metadata.GetPropertyDefinition((PropertyDefinitionHandle) handle);
                declaringType = metadata.GetMethodDefinition(pd.GetAccessors().GetAny()).GetDeclaringType();
                if (fullName && !declaringType.IsNil)
                {
                    return ToCSharpString(metadata, declaringType, fullName, omitGenerics) + "." + metadata.GetString(pd.Name);
                }
                return metadata.GetString(pd.Name);
            default:
                return "";
        }
    }

    public string GetTooltip(IEntity entity)
    {
        return "";
    }

    public string MethodToString(IMethod method, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
    {
        if (method == null)
        {
            throw new ArgumentNullException(nameof(method));
        }
        return EntityToString(method, includeDeclaringTypeName, includeNamespace, includeNamespaceOfDeclaringTypeName);
    }

    public string PropertyToString(IProperty property, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
    {
        if (property == null)
        {
            throw new ArgumentNullException(nameof(property));
        }
        return EntityToString(property, includeDeclaringTypeName, includeNamespace, includeNamespaceOfDeclaringTypeName);
    }

    public bool ShowMember(IEntity member)
    {
        return true;
    }

    public string TypeToString(IType type, bool includeNamespace)
    {
        if (type == null)
        {
            throw new ArgumentNullException(nameof(type));
        }
        var ambience = CreateAmbience();
        // Do not forget to update CSharpAmbienceTests.ILSpyMainTreeViewFlags, if this ever changes.
        if (includeNamespace)
        {
            ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedTypeNames;
            ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedEntityNames;
        }
        if (type is ITypeDefinition definition)
        {
            return ambience.ConvertSymbol(definition);
            // HACK : UnknownType is not supported by CSharpAmbience.
        }
        else if (type.Kind == TypeKind.Unknown)
        {
            return (includeNamespace ? type.FullName : type.Name)
                + (type.TypeParameterCount > 0 ? "<" + string.Join(", ", type.TypeArguments.Select(t => t.Name)) + ">" : "");
        }
        else
        {
            return ambience.ConvertType(type);
        }
    }

    private static string EntityToString(IEntity entity, bool includeDeclaringTypeName, bool includeNamespace, bool includeNamespaceOfDeclaringTypeName)
    {
        // Do not forget to update CSharpAmbienceTests, if this ever changes.
        var ambience = CreateAmbience();
        ambience.ConversionFlags |= ConversionFlags.ShowReturnType | ConversionFlags.ShowParameterList | ConversionFlags.ShowParameterModifiers;
        if (includeDeclaringTypeName)
        {
            ambience.ConversionFlags |= ConversionFlags.ShowDeclaringType;
        }
        if (includeNamespace)
        {
            ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedTypeNames;
        }
        if (includeNamespaceOfDeclaringTypeName)
        {
            ambience.ConversionFlags |= ConversionFlags.UseFullyQualifiedEntityNames;
        }
        return ambience.ConvertSymbol(entity);
    }

    private static CSharpAmbience CreateAmbience()
    {
        return new CSharpAmbience
        {
            ConversionFlags = ConversionFlags.ShowTypeParameterList | ConversionFlags.PlaceReturnTypeAfterParameterList | ConversionFlags.UseNullableSpecifierForValueTypes
        };
    }

    private string ToCSharpString(MetadataReader metadata, TypeDefinitionHandle handle, bool fullName, bool omitGenerics)
    {
        var builder = new StringBuilder();
        var currentTypeDefHandle = handle;
        var typeDef = metadata.GetTypeDefinition(currentTypeDefHandle);

        while (!currentTypeDefHandle.IsNil)
        {
            if (builder.Length > 0)
            {
                builder.Insert(0, '.');
            }
            typeDef = metadata.GetTypeDefinition(currentTypeDefHandle);
            string part = ReflectionHelper.SplitTypeParameterCountFromReflectionName(metadata.GetString(typeDef.Name), out int typeParamCount);
            var genericParams = typeDef.GetGenericParameters();
            if (!omitGenerics && genericParams.Count > 0)
            {
                builder.Insert(0, '>');
                int firstIndex = genericParams.Count - typeParamCount;
                for (int i = genericParams.Count - 1; i >= genericParams.Count - typeParamCount; i--)
                {
                    builder.Insert(0, metadata.GetString(metadata.GetGenericParameter(genericParams[i]).Name));
                    builder.Insert(0, i == firstIndex ? '<' : ',');
                }
            }
            builder.Insert(0, part);
            currentTypeDefHandle = typeDef.GetDeclaringType();
            if (!fullName)
            {
                break;
            }
        }

        if (fullName && !typeDef.Namespace.IsNil)
        {
            builder.Insert(0, '.');
            builder.Insert(0, metadata.GetString(typeDef.Namespace));
        }

        return builder.ToString();
    }

    public CodeMappingInfo GetCodeMappingInfo(MetadataFile module, EntityHandle member)
    {
        return CSharpDecompiler.GetCodeMappingInfo(module, member);
    }
}

