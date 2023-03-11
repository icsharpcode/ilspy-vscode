import {
	CancellationToken,
	HandlerResult,
	ParameterStructures,
	RequestHandler,
	RequestType,
  } from "vscode-languageclient";

import AssemblyRequestParams from "./AssemblyRequestParams";
  
export interface ListAssemblyReferencesParams extends AssemblyRequestParams {}
  
export interface ListAssemblyReferencesResponse {
  references: string[];
}
  
export namespace ListAssemblyReferencesRequest {
  export const type = new RequestType<
	  ListAssemblyReferencesParams,
	  ListAssemblyReferencesResponse | null,
	  never
	>("ilspy/listAssemblyReferences", ParameterStructures.byName);
	export type HandlerSignature = RequestHandler<
	  ListAssemblyReferencesParams,
	  ListAssemblyReferencesResponse | null,
	  void
	>;
	export type MiddlewareSignature = (
	  token: CancellationToken,
	  next: HandlerSignature
	) => HandlerResult<ListAssemblyReferencesResponse | null, void>;
}
