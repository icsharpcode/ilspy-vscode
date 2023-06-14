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
import AssemblyData from "./AssemblyData";

export interface InitWithAssembliesParams {
  assemblyPaths: string[];
}

export interface InitWithAssembliesResponse {
  loadedAssemblies?: AssemblyData[];
}

export namespace InitWithAssembliesRequest {
  export const type = new RequestType<
    InitWithAssembliesParams,
    InitWithAssembliesResponse | null,
    never
  >("ilspy/initWithAssemblies", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    InitWithAssembliesParams,
    InitWithAssembliesResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<InitWithAssembliesResponse | null, void>;
}
