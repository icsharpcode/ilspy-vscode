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

export interface RemoveAssemblyParams extends AssemblyRequestParams {}

export interface RemoveAssemblyResponse {
  removed: boolean;
}

export namespace RemoveAssemblyRequest {
  export const type = new RequestType<
    RemoveAssemblyParams,
    RemoveAssemblyResponse | null,
    never
  >("ilspy/removeAssembly", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    RemoveAssemblyParams,
    RemoveAssemblyResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<RemoveAssemblyResponse | null, void>;
}
