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
import { Node, SearchMatch } from "../extension-types";

export interface SearchParams {
  term: string;
  // If set, results found by a literal search include the match location in code of this language.
  outputLanguage?: string;
}

export type SearchResultNode = Node & { match?: SearchMatch };

export interface SearchResponse {
  results: SearchResultNode[];
  shouldUpdateAssemblyList: boolean;
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
