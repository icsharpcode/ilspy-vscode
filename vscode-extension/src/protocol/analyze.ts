/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2024 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  CancellationToken,
  HandlerResult,
  ParameterStructures,
  RequestHandler,
  RequestType,
} from "vscode-languageclient";
import Node from "./Node";
import NodeMetadata from "./NodeMetadata";

export interface AnalyzeParams {
  node?: NodeMetadata;
}

export interface AnalyzeResponse {
  results: Node[];
}

export namespace AnalyzeRequest {
  export const type = new RequestType<
    AnalyzeParams,
    AnalyzeResponse | null,
    never
  >("ilspy/analyze", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    AnalyzeParams,
    AnalyzeResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<AnalyzeResponse | null, void>;
}
