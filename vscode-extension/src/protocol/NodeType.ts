/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export enum NodeType {
  Unknown = 0,
  Assembly = 1,
  Namespace = 2,
  Class = 3,
  Interface = 4,
  Struct = 5,
  Enum = 6,
  Delegate = 7,
  Event = 8,
  Field = 9,
  Method = 10,
  Const = 11,
  Property = 12,
  AssemblyReference = 13,
  ReferencesRoot = 14,
  Analyzer = 15,
  BaseTypes = 16,
  DerivedTypes = 17,
  NuGetPackage = 18,
  PackageFolder = 19,
  Resource = 20,
}
