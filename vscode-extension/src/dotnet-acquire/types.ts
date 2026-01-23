/*------------------------------------------------------------------------------------------------
 *  Copyright (c) ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

export enum AcquireErrorConfiguration {
  DisplayAllErrorPopups = 0,
  DisableErrorPopups = 1,
}

export interface IDotnetAcquireContext {
  version: string;
  requestingExtensionId?: string;
  errorConfiguration?: AcquireErrorConfiguration;
}

export interface IDotnetAcquireResult {
  dotnetPath: string;
}
