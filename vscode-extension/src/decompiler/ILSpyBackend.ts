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
import {
  DecompileAssemblyParams,
  DecompileAssemblyRequest,
} from "../protocol/decompileAssembly";
import {
  DecompileMemberParams,
  DecompileMemberRequest,
} from "../protocol/decompileMember";
import DecompileResponse from "../protocol/DecompileResponse";
import {
  DecompileTypeParams,
  DecompileTypeRequest,
} from "../protocol/decompileType";
import {
  ListMembersParams,
  ListMembersRequest,
  ListMembersResponse,
} from "../protocol/listMembers";
import {
  ListNamespacesParams,
  ListNamespacesRequest,
  ListNamespacesResponse,
} from "../protocol/listNamespaces";
import {
  ListAssemblyReferencesParams,
  ListAssemblyReferencesRequest,
  ListAssemblyReferencesResponse,
} from "../protocol/listAssemblyReferences";
import {
  ListTypesParams,
  ListTypesRequest,
  ListTypesResponse,
} from "../protocol/listTypes";
import {
  RemoveAssemblyParams,
  RemoveAssemblyRequest,
  RemoveAssemblyResponse,
} from "../protocol/removeAssembly";
import IILSpyBackend from "./IILSpyBackend";
import AssemblyData from "../protocol/AssemblyData";
import {
  SearchParams,
  SearchRequest,
  SearchResponse,
} from "../protocol/search";
import Node from "../protocol/Node";

export default class ILSpyBackend implements IILSpyBackend {
  public readonly assemblies = new Map<string, AssemblyData>();

  constructor(private languageClient: LanguageClient) {}

  public static getExecutable(context: vscode.ExtensionContext) {
    return context.asAbsolutePath(
      path.join("bin", "ilspy-backend", "ILSpy.Backend.dll")
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

  public sendDecompileAssembly(
    params: DecompileAssemblyParams
  ): Promise<DecompileResponse | null> {
    return this.languageClient.sendRequest(
      DecompileAssemblyRequest.type,
      params
    );
  }

  public sendDecompileMember(
    params: DecompileMemberParams
  ): Promise<DecompileResponse | null> {
    return this.languageClient.sendRequest(DecompileMemberRequest.type, params);
  }

  public sendDecompileType(
    params: DecompileTypeParams
  ): Promise<DecompileResponse | null> {
    return this.languageClient.sendRequest(DecompileTypeRequest.type, params);
  }

  public sendListMembers(
    params: ListMembersParams
  ): Promise<ListMembersResponse | null> {
    return this.languageClient.sendRequest(ListMembersRequest.type, params);
  }

  public sendListNamespaces(
    params: ListNamespacesParams
  ): Promise<ListNamespacesResponse | null> {
    return this.languageClient.sendRequest(ListNamespacesRequest.type, params);
  }

  public sendListAssemblyReferences(
    params: ListAssemblyReferencesParams
  ): Promise<ListAssemblyReferencesResponse | null> {
    return this.languageClient.sendRequest(ListAssemblyReferencesRequest.type, params);
  }

  public sendListTypes(
    params: ListTypesParams
  ): Promise<ListTypesResponse | null> {
    return this.languageClient.sendRequest(ListTypesRequest.type, params);
  }

  public sendSearch(params: SearchParams): Promise<SearchResponse | null> {
    return this.languageClient.sendRequest(SearchRequest.type, params);
  }
}
