/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

export interface Request {
    command: string;
    data?: any;
    onSuccess(value: any): void;
    onError(err: any): void;
    startTime?: number;
    endTime?: number;
}
