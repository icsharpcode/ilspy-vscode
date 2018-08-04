/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// from https://raw.githubusercontent.com/dotnet/corefx/28cb4c8917c25033bb136055c3c2462907a281fe/src/System.Reflection.Metadata/src/System/Reflection/Metadata/HandleKind.cs
export enum TokenType
{
    ModuleDefinition = 0,
    TypeReference = 1,
    TypeDefinition = 2,
    FieldDefinition = 4,
    MethodDefinition = 6,
    Parameter = 8,
    InterfaceImplementation = 9,
    MemberReference = 10,
    Constant = 11,
    CustomAttribute = 12,
    DeclarativeSecurityAttribute = 14,
    StandaloneSignature = 17,
    EventDefinition = 20,
    PropertyDefinition = 23,
    MethodImplementation = 25,
    ModuleReference = 26,
    TypeSpecification = 27,
    AssemblyDefinition = 0x20,
    AssemblyFile = 38,
    AssemblyReference = 35,
    ExportedType = 39,
    GenericParameter = 42,
    MethodSpecification = 43,
    GenericParameterConstraint = 44,
    ManifestResource = 40,
    Document = 48,
    MethodDebugInformation = 49,
    LocalScope = 50,
    LocalVariable = 51,
    LocalConstant = 52,
    ImportScope = 53,
    CustomDebugInformation = 55,
    NamespaceDefinition = 124,
    UserString = 112,
    String = 120,
    Blob = 113,
    Guid = 114
}

