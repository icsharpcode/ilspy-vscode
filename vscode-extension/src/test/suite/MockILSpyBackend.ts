import IILSpyBackend from "../../decompiler/IILSpyBackend";
import {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../../protocol/addAssembly";
import { DecompileAssemblyParams } from "../../protocol/decompileAssembly";
import { DecompileMemberParams } from "../../protocol/decompileMember";
import DecompileResponse from "../../protocol/DecompileResponse";
import { DecompileTypeParams } from "../../protocol/decompileType";
import {
  ListMembersParams,
  ListMembersResponse,
} from "../../protocol/listMembers";
import {
  ListNamespacesParams,
  ListNamespacesResponse,
} from "../../protocol/listNamespaces";
import {
  ListTypesParams,
  ListTypesResponse,
  ListTypesRequest,
} from "../../protocol/listTypes";
import {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../../protocol/removeAssembly";

export default class MockILSpyBackend implements IILSpyBackend {
  public readonly assemblyPaths: Set<string> = new Set<string>();

  public sendAddAssembly(
    params: AddAssemblyParams
  ): Promise<AddAssemblyResponse | null> {
    return Promise.resolve(null);
  }

  public sendRemoveAssembly(
    params: RemoveAssemblyParams
  ): Promise<RemoveAssemblyResponse | null> {
    return Promise.resolve(null);
  }

  public sendDecompileAssembly(
    params: DecompileAssemblyParams
  ): Promise<DecompileResponse | null> {
    return Promise.resolve(null);
  }

  public sendDecompileMember(
    params: DecompileMemberParams
  ): Promise<DecompileResponse | null> {
    return Promise.resolve(null);
  }

  public sendDecompileType(
    params: DecompileTypeParams
  ): Promise<DecompileResponse | null> {
    return Promise.resolve(null);
  }

  public sendListMembers(
    params: ListMembersParams
  ): Promise<ListMembersResponse | null> {
    return Promise.resolve(null);
  }

  public sendListNamespaces(
    params: ListNamespacesParams
  ): Promise<ListNamespacesResponse | null> {
    return Promise.resolve(null);
  }

  public sendListTypes(
    params: ListTypesParams
  ): Promise<ListTypesResponse | null> {
    return Promise.resolve(null);
  }
}
