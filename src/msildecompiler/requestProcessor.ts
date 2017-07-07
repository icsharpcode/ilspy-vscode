/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

'use strict';

import { Logger } from '../logger';

export interface Request {
    command: string;
    data?: any;
    onSuccess(value: any): void;
    onError(err: any): void;
    startTime?: number;
    endTime?: number;
}

export class RequestProcessor {

    public constructor(
        private _logger: Logger,
        private _makeRequest: (request: Request) => number) {
    }

    /**
     * Process a request and send it to the MsilDecompiler server.
     */
    public processRequest(request: Request) {

        this._logger.appendLine(`Processing ${request.command} request`);
        this._logger.increaseIndent();

        const id = this._makeRequest(request);

        this._logger.decreaseIndent();
    }
}