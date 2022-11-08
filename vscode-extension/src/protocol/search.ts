/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  CancellationToken,
  HandlerResult,
  ParameterStructures,
  RequestHandler,
  RequestType,
} from "vscode-languageclient";
import AssemblyRequestParams from "./AssemblyRequestParams";
import NodeData from "./NodeData";

export interface SearchParams {
  term: string;
}

export interface SearchResponse {
  results: NodeData[];
}

export namespace SearchRequest {
  export const type = new RequestType<
    SearchParams,
    SearchResponse | null,
    never
  >("ilspy/search", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    SearchParams,
    SearchResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<SearchResponse | null, void>;
}
