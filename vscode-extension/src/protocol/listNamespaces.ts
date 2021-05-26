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

export interface ListNamespacesParams extends AssemblyRequestParams {}

export interface ListNamespacesResponse {
  namespaces: string[];
}

export namespace ListNamespacesRequest {
  export const type = new RequestType<
    ListNamespacesParams,
    ListNamespacesResponse | null,
    never
  >("ilspy/listNamespaces", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ListNamespacesParams,
    ListNamespacesResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<ListNamespacesResponse | null, void>;
}
