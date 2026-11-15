/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { ParameterStructures, RequestType } from "vscode-languageclient";
import { Node, NodeMetadata } from "../extension-types";

export interface SymbolNavigationParams {
  // Node of the decompiled document and the zero-based position in its code
  nodeMetadata: NodeMetadata;
  outputLanguage: string;
  line: number;
  column: number;
}

export interface CodeLocation {
  // Node whose decompiled code contains the location, not set for the requesting document itself
  node?: Node;
  line: number;
  column: number;
  length: number;
}

export interface CodeLocationsResponse {
  locations?: CodeLocation[];
  shouldUpdateAssemblyList: boolean;
}

export namespace FindDefinitionsRequest {
  export const type = new RequestType<
    SymbolNavigationParams,
    CodeLocationsResponse | null,
    never
  >("ilspy/findDefinitions", ParameterStructures.byName);
}

export namespace FindImplementationsRequest {
  export const type = new RequestType<
    SymbolNavigationParams,
    CodeLocationsResponse | null,
    never
  >("ilspy/findImplementations", ParameterStructures.byName);
}
