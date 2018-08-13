/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// This should be the same as ICSharpCode.Decompiler.TypeSystem.TypeKind in ICSharpCode.Decompiler.dll
export enum MemberSubKind
{
    Other,
    Class,
    Interface,
    Struct,
    Delegate,
    Enum,
    Void,
    Unknown,
    Null,
    None,
    Dynamic,
    UnboundTypeArgument,
    TypeParameter,
    Array,
    Pointer,
    ByReference,
    Anonymous,
    Intersection,
    ArgList,
    Tuple,
    ModOpt,
    ModReq
}