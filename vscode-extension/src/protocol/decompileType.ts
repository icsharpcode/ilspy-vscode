/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
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
import DecompileResponse from "./DecompileResponse";

export interface DecompileTypeParams extends AssemblyRequestParams {
  Handle: number;
}

export namespace DecompileTypeRequest {
  export const type = new RequestType<
    DecompileTypeParams,
    DecompileResponse | null,
    never
  >("ilspy/decompileType", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    DecompileTypeParams,
    DecompileResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<DecompileResponse | null, void>;
}
