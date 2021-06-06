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
import MemberData from "./MemberData";

export interface ListTypesParams extends AssemblyRequestParams {
  namespace: string;
}

export interface ListTypesResponse {
  types: MemberData[];
}

export namespace ListTypesRequest {
  export const type = new RequestType<
    ListTypesParams,
    ListTypesResponse | null,
    never
  >("ilspy/listTypes", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ListTypesParams,
    ListTypesResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<ListTypesResponse | null, void>;
}
