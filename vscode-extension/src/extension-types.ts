export enum NodeFlags {
  None = 0,
  CompilerGenerated = 1,
  AutoLoaded = 2,
}

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

export enum AvailableNodeCommands {
  None = 0,
  Decompile = 1,
  Analyze = 2,
  Export = 4,
  ManageRootEntries = 8,
}

export interface NodeMetadata {
  assemblyPath: string;
  type: NodeType;
  name: string;
  bundledAssemblyName?: string;
  symbolToken: number;
  parentSymbolToken: number;
  subType?: string;
  availableCommands: AvailableNodeCommands;
}

export enum SymbolModifiers {
  None = 0,
  Public = 1,
  Internal = 2,
  Protected = 4,
  Private = 8,
  Sealed = 16,
  Abstract = 32,
  Virtual = 64,
  Override = 128,
  Static = 256,
  ReadOnly = 512,
}

export interface Node {
  metadata?: NodeMetadata;
  displayName: string;
  description: string;
  mayHaveChildren: boolean;
  modifiers: SymbolModifiers;
  flags: NodeFlags;
}

export enum LanguageName {
  IL = "il",
  CSharp_1 = "cs-1",
  CSharp_2 = "cs-2",
  CSharp_3 = "cs-3",
  CSharp_4 = "cs-4",
  CSharp_5 = "cs-5",
  CSharp_6 = "cs-6",
  CSharp_7 = "cs-7",
  CSharp_7_1 = "cs-7.1",
  CSharp_7_2 = "cs-7.2",
  CSharp_7_3 = "cs-7.3",
  CSharp_8 = "cs-8",
  CSharp_9 = "cs-9",
  CSharp_10 = "cs-10",
  CSharp_11 = "cs-11",
  CSharp_12 = "cs-12",
  CSharp_13 = "cs-13",
  CSharp_14 = "cs-14",
}
