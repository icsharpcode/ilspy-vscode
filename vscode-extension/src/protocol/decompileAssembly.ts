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

export interface DecompileAssemblyParams extends AssemblyRequestParams {}

export namespace DecompileAssemblyRequest {
  export const type = new RequestType<
    DecompileAssemblyParams,
    DecompileResponse | null,
    never
  >("ilspy/decompileAssembly", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    DecompileAssemblyParams,
    DecompileResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<DecompileResponse | null, void>;
}
