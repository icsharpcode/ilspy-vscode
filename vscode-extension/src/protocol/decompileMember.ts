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

export interface DecompileMemberParams extends AssemblyRequestParams {
  type: number;
  member: number;
}

export namespace DecompileMemberRequest {
  export const type = new RequestType<
    DecompileMemberParams,
    DecompileResponse | null,
    never
  >("ilspy/decompileMember", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    DecompileMemberParams,
    DecompileResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<DecompileResponse | null, void>;
}
