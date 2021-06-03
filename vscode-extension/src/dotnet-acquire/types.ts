/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

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
