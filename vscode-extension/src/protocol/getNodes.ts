/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2023 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  CancellationToken,
  HandlerResult,
  ParameterStructures,
  RequestHandler,
  RequestType,
} from "vscode-languageclient";
import { Node, NodeMetadata } from "../extension-types";

export interface GetNodesParams {
  nodeMetadata?: NodeMetadata;
}

export interface GetNodesResponse {
  nodes?: Node[];
  shouldUpdateAssemblyList: boolean;
}

export namespace GetNodesRequest {
  export const type = new RequestType<
    GetNodesParams,
    GetNodesResponse | null,
    never
  >("ilspy/getNodes", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    GetNodesParams,
    GetNodesResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<GetNodesResponse | null, void>;
}
