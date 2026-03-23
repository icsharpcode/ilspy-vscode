/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2025 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import {
  CancellationToken,
  HandlerResult,
  ParameterStructures,
  RequestHandler,
  RequestType,
} from "vscode-languageclient";
import NodeMetadata from "./NodeMetadata";

export interface ExportNodeParams {
  nodeMetadata: NodeMetadata;
  outputLanguage: string;
  outputDirectory: string;
  includeCompilerGenerated: boolean;
}

export default interface ExportNodeResponse {
  succeeded: boolean;
  outputDirectory?: string;
  filesWritten: number;
  errorCount: number;
  errorMessage?: string;
  shouldUpdateAssemblyList: boolean;
}

export namespace ExportNodeRequest {
  export const type = new RequestType<
    ExportNodeParams,
    ExportNodeResponse | null,
    never
  >("ilspy/exportNode", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ExportNodeParams,
    ExportNodeResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<ExportNodeResponse | null, void>;
}
