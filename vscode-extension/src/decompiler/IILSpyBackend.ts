/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../protocol/addAssembly";
import AssemblyData from "../protocol/AssemblyData";
import { DecompileAssemblyParams } from "../protocol/decompileAssembly";
import { DecompileMemberParams } from "../protocol/decompileMember";
import DecompileResponse from "../protocol/DecompileResponse";
import { DecompileTypeParams } from "../protocol/decompileType";
import {
  ListMembersParams,
  ListMembersResponse,
} from "../protocol/listMembers";
import {
  ListNamespacesParams,
  ListNamespacesResponse,
} from "../protocol/listNamespaces";
import {
  ListAssemblyReferencesParams,
  ListAssemblyReferencesResponse,
} from "../protocol/listAssemblyReferences";
import { ListTypesParams, ListTypesResponse } from "../protocol/listTypes";
import Node from "../protocol/Node";
import {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";
import { SearchParams, SearchResponse } from "../protocol/search";
import { DecompileNodeParams } from "../protocol/decompileNode";
import { GetNodesParams, GetNodesResponse } from "../protocol/getNodes";

export default interface IILSpyBackend {
  sendAddAssembly(
    params: AddAssemblyParams
  ): Promise<AddAssemblyResponse | null>;

  sendRemoveAssembly(
    params: RemoveAssemblyParams
  ): Promise<RemoveAssemblyResponse | null>;

  sendDecompileAssembly(
    params: DecompileAssemblyParams
  ): Promise<DecompileResponse | null>;

  sendDecompileMember(
    params: DecompileMemberParams
  ): Promise<DecompileResponse | null>;

  sendDecompileType(
    params: DecompileTypeParams
  ): Promise<DecompileResponse | null>;

  sendDecompileNode(
    params: DecompileNodeParams
  ): Promise<DecompileResponse | null>;

  sendGetNodes(params: GetNodesParams): Promise<GetNodesResponse | null>;

  sendListMembers(
    params: ListMembersParams
  ): Promise<ListMembersResponse | null>;

  sendListNamespaces(
    params: ListNamespacesParams
  ): Promise<ListNamespacesResponse | null>;

  sendListAssemblyReferences(
    params: ListAssemblyReferencesParams
  ): Promise<ListAssemblyReferencesResponse | null>;

  sendListTypes(params: ListTypesParams): Promise<ListTypesResponse | null>;

  sendSearch(params: SearchParams): Promise<SearchResponse | null>;
}
