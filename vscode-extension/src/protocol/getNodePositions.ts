/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  CancellationToken,
  HandlerResult,
  ParameterStructures,
  RequestHandler,
  RequestType,
} from "vscode-languageclient";
import { NodeMetadata } from "../extension-types";

export interface NodePosition {
  symbolToken: number;
  startLine: number;
  startCharacter: number;
  endLine: number;
  endCharacter: number;
}

export interface GetNodePositionsParams {
  nodeMetadata: NodeMetadata;
  outputLanguage: string;
}

export interface GetNodePositionsResponse {
  positions?: NodePosition[];
  shouldUpdateAssemblyList: boolean;
}

export namespace GetNodePositionsRequest {
  export const type = new RequestType<
    GetNodePositionsParams,
    GetNodePositionsResponse | null,
    never
  >("ilspy/getNodePositions", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    GetNodePositionsParams,
    GetNodePositionsResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature,
  ) => HandlerResult<GetNodePositionsResponse | null, void>;
}
