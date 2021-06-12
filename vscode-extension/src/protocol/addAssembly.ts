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
import AssemblyData from "./AssemblyData";
import AssemblyRequestParams from "./AssemblyRequestParams";

export interface AddAssemblyParams extends AssemblyRequestParams {}

export interface AddAssemblyResponse {
  added: boolean;
  assemblyData?: AssemblyData;
}

export namespace AddAssemblyRequest {
  export const type = new RequestType<
    AddAssemblyParams,
    AddAssemblyResponse | null,
    never
  >("ilspy/addAssembly", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    AddAssemblyParams,
    AddAssemblyResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<AddAssemblyResponse | null, void>;
}
