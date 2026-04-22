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
import NodeMetadata from "./NodeMetadata";
import Node from "./Node";

export interface ResolveNodePathParams {
  nodeMetadata?: NodeMetadata;
}

export interface ResolveNodePathResponse {
  nodePath?: Node[];
  leafNode?: Node;
  shouldUpdateAssemblyList: boolean;
}

export namespace ResolveNodePathRequest {
  export const type = new RequestType<
    ResolveNodePathParams,
    ResolveNodePathResponse | null,
    never
  >("ilspy/resolveNodePath", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ResolveNodePathParams,
    ResolveNodePathResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature,
  ) => HandlerResult<ResolveNodePathResponse | null, void>;
}
