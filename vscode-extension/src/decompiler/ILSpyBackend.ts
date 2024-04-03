/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2021 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as vscode from "vscode";
import * as path from "path";
import { LanguageClient } from "vscode-languageclient/node";
import {
  AddAssemblyParams,
  AddAssemblyRequest,
  AddAssemblyResponse,
} from "../protocol/addAssembly";
import DecompileResponse from "../protocol/DecompileResponse";
import {
  RemoveAssemblyParams,
  RemoveAssemblyRequest,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";
import IILSpyBackend from "./IILSpyBackend";
import {
  SearchParams,
  SearchRequest,
  SearchResponse,
} from "../protocol/search";
import {
  DecompileNodeParams,
  DecompileNodeRequest,
} from "../protocol/decompileNode";
import {
  GetNodesParams,
  GetNodesRequest,
  GetNodesResponse,
} from "../protocol/getNodes";
import {
  InitWithAssembliesParams,
  InitWithAssembliesRequest,
  InitWithAssembliesResponse,
} from "../protocol/initWithAssemblies";
import {
  AnalyzeParams,
  AnalyzeRequest,
  AnalyzeResponse,
} from "../protocol/analyze";

export default class ILSpyBackend implements IILSpyBackend {
  constructor(private languageClient: LanguageClient) {}

  public static getExecutable(context: vscode.ExtensionContext) {
    return context.asAbsolutePath(
      path.join("bin", "ilspy-backend", "ILSpyX.Backend.LSP.dll")
    );
  }

  public sendInitWithAssemblies(
    params: InitWithAssembliesParams
  ): Promise<InitWithAssembliesResponse | null> {
    return this.languageClient.sendRequest(
      InitWithAssembliesRequest.type,
      params
    );
  }

  public sendAddAssembly(
    params: AddAssemblyParams
  ): Promise<AddAssemblyResponse | null> {
    return this.languageClient.sendRequest(AddAssemblyRequest.type, params);
  }

  public sendRemoveAssembly(
    params: RemoveAssemblyParams
  ): Promise<RemoveAssemblyResponse | null> {
    return this.languageClient.sendRequest(RemoveAssemblyRequest.type, params);
  }

  public sendDecompileNode(
    params: DecompileNodeParams
  ): Promise<DecompileResponse | null> {
    return this.languageClient.sendRequest(DecompileNodeRequest.type, params);
  }

  public sendGetNodes(
    params: GetNodesParams
  ): Promise<GetNodesResponse | null> {
    return this.languageClient.sendRequest(GetNodesRequest.type, params);
  }

  public sendSearch(params: SearchParams): Promise<SearchResponse | null> {
    return this.languageClient.sendRequest(SearchRequest.type, params);
  }

  sendAnalyze(params: AnalyzeParams): Promise<AnalyzeResponse | null> {
    return this.languageClient.sendRequest(AnalyzeRequest.type, params);
  }
}
