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
import DecompileResponse from "./DecompileResponse";
import NodeMetadata from "./NodeMetadata";

export interface DecompileNodeParams {
  nodeMetadata: NodeMetadata;
  language: string;
}

export namespace DecompileNodeRequest {
  export const type = new RequestType<
    DecompileNodeParams,
    DecompileResponse | null,
    never
  >("ilspy/decompileNode", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    DecompileNodeParams,
    DecompileResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<DecompileResponse | null, void>;
}
