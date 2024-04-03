/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  AddAssemblyParams,
  AddAssemblyResponse,
} from "../protocol/addAssembly";
import DecompileResponse from "../protocol/DecompileResponse";
import {
  RemoveAssemblyParams,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";
import { SearchParams, SearchResponse } from "../protocol/search";
import { DecompileNodeParams } from "../protocol/decompileNode";
import { GetNodesParams, GetNodesResponse } from "../protocol/getNodes";
import {
  InitWithAssembliesParams,
  InitWithAssembliesResponse,
} from "../protocol/initWithAssemblies";
import { AnalyzeParams, AnalyzeResponse } from "../protocol/analyze";

export default interface IILSpyBackend {
  sendInitWithAssemblies(
    params: InitWithAssembliesParams
  ): Promise<InitWithAssembliesResponse | null>;

  sendAddAssembly(
    params: AddAssemblyParams
  ): Promise<AddAssemblyResponse | null>;

  sendRemoveAssembly(
    params: RemoveAssemblyParams
  ): Promise<RemoveAssemblyResponse | null>;

  sendDecompileNode(
    params: DecompileNodeParams
  ): Promise<DecompileResponse | null>;

  sendGetNodes(params: GetNodesParams): Promise<GetNodesResponse | null>;

  sendSearch(params: SearchParams): Promise<SearchResponse | null>;
  sendAnalyze(params: AnalyzeParams): Promise<AnalyzeResponse | null>;
}
