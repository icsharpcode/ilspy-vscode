import IILSpyBackend from "../../decompiler/IILSpyBackend";
import {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../../protocol/addAssembly";
import AssemblyData from "../../protocol/AssemblyData";
import { DecompileNodeParams } from "../../protocol/decompileNode";
import DecompileResponse from "../../protocol/DecompileResponse";
import { DecompileTypeParams } from "../../protocol/decompileType";
import { GetNodesParams, GetNodesResponse } from "../../protocol/getNodes";
import {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../../protocol/removeAssembly";
import { SearchParams, SearchResponse } from "../../protocol/search";

export default class MockILSpyBackend implements IILSpyBackend {
  public readonly assemblies = new Map<string, AssemblyData>();

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

  public sendDecompileNode(
    params: DecompileNodeParams
  ): Promise<DecompileResponse | null> {
    return Promise.resolve(null);
  }

  public sendGetNodes(
    params: GetNodesParams
  ): Promise<GetNodesResponse | null> {
    return Promise.resolve(null);
  }

  public sendSearch(params: SearchParams): Promise<SearchResponse | null> {
    return Promise.resolve(null);
  }
}
