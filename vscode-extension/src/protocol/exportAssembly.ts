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
import ExportAssemblyResponse from "./ExportAssemblyResponse";
import NodeMetadata from "./NodeMetadata";

export interface ExportAssemblyParams {
  nodeMetadata: NodeMetadata;
  outputLanguage: string;
  outputDirectory: string;
  includeCompilerGenerated: boolean;
}

export namespace ExportAssemblyRequest {
  export const type = new RequestType<
    ExportAssemblyParams,
    ExportAssemblyResponse | null,
    never
  >("ilspy/exportAssembly", ParameterStructures.byName);
  export type HandlerSignature = RequestHandler<
    ExportAssemblyParams,
    ExportAssemblyResponse | null,
    void
  >;
  export type MiddlewareSignature = (
    token: CancellationToken,
    next: HandlerSignature
  ) => HandlerResult<ExportAssemblyResponse | null, void>;
}
