/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../protocol/addAssembly";
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
import { ListTypesParams, ListTypesResponse } from "../protocol/listTypes";
import {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";

export default interface IILSpyBackend {
  readonly assemblyPaths: Set<string>;

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

  sendListMembers(
    params: ListMembersParams
  ): Promise<ListMembersResponse | null>;

  sendListNamespaces(
    params: ListNamespacesParams
  ): Promise<ListNamespacesResponse | null>;

  sendListTypes(params: ListTypesParams): Promise<ListTypesResponse | null>;
}
