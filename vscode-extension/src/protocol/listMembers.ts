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

export interface ListMembersParams extends AssemblyRequestParams {
  handle: number;
}

export interface ListMembersResponse {
  members: MemberData[];
}

export namespace ListMembersRequest {
  export const type = new RequestType<
    ListMembersParams,
    ListMembersResponse | null,
    never
  >("ilspy/listMembers", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ListMembersParams,
    ListMembersResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<ListMembersResponse | null, void>;
}
