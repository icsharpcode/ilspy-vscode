using ICSharpCode.Decompiler.TypeSystem;
using ILSpy.Backend.Model;
using System;

namespace ILSpy.Backend.TreeProviders;

public static class NodeTypeHelper
{
    public static bool IsTypeNode(NodeType nodeType) =>
        nodeType is NodeType.Class or NodeType.Enum or NodeType.Delegate or NodeType.Interface or NodeType.Struct;

    public static bool IsMemberNode(NodeType nodeType) =>
        nodeType is NodeType.Event or NodeType.Const or NodeType.Field or NodeType.Method or NodeType.Property;

    public static NodeType GetNodeTypeFromTypeKind(TypeKind typeKind) => typeKind switch
    {
        TypeKind.Class => NodeType.Class,
        TypeKind.Delegate => NodeType.Delegate,
        TypeKind.Enum => NodeType.Enum,
        TypeKind.Interface => NodeType.Interface,
        TypeKind.Struct => NodeType.Struct,
        _ => NodeType.Unknown
    };

    public static NodeType GetNodeTypeFromEntity(IEntity entity) => entity switch
    {
        ITypeDefinition typeDefinition => GetNodeTypeFromTypeKind(typeDefinition.Kind),
        IMethod => NodeType.Method,
        IField => NodeType.Field,
        IEvent => NodeType.Event,
        IProperty => NodeType.Property,
        _ => NodeType.Unknown
    };

    public static SymbolModifiers GetSymbolModifiers(IEntity entity)
    {
        SymbolModifiers modifiers = SymbolModifiers.None;

        switch (entity)
        {
            case ITypeDefinition typeDefinition:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, typeDefinition.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, typeDefinition.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, typeDefinition.IsReadOnly);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, typeDefinition.IsSealed);
                break;

            case IField field:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, field.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, field.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, field.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, field.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, field.IsSealed);
                MapSymbolModifier(ref modifiers, SymbolModifiers.ReadOnly, field.IsReadOnly);
                break;

            case IMember member:
                MapSymbolModifier(ref modifiers, SymbolModifiers.Abstract, member.IsAbstract);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Virtual, member.IsVirtual);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Override, member.IsOverride);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Static, member.IsStatic);
                MapSymbolModifier(ref modifiers, SymbolModifiers.Sealed, member.IsSealed);
                break;
        }

        MapSymbolModifierFromAccessibility(ref modifiers, entity.Accessibility);

        return modifiers;
    }

    public static void MapSymbolModifier(ref SymbolModifiers modifiers, SymbolModifiers modifier, bool condition)
    {
        if (condition)
        {
            modifiers |= modifier;
        }
    }

    public static void MapSymbolModifierFromAccessibility(ref SymbolModifiers modifiers, Accessibility accessibility)
    {
        switch (accessibility)
        {
            case Accessibility.Private:
                modifiers |= SymbolModifiers.Private;
                break;
            case Accessibility.ProtectedAndInternal:
                modifiers |= SymbolModifiers.Protected | SymbolModifiers.Private;
                break;
            case Accessibility.Protected:
                modifiers |= SymbolModifiers.Protected;
                break;
            case Accessibility.Internal:
                modifiers |= SymbolModifiers.Internal;
                break;
            case Accessibility.ProtectedOrInternal:
                modifiers |= SymbolModifiers.Protected | SymbolModifiers.Internal;
                break;
            case Accessibility.Public:
                modifiers |= SymbolModifiers.Public;
                break;
            default:
                break;
        }
    }
}

