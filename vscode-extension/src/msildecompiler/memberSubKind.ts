/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// This should be the same as defined in ILSpy.Host back end
export enum MemberSubKind
{
    None = 0x0000,
    Class = 0x0001,
    Enum = 0x0002,
    Interface = 0x0003,
    Structure = 0x0004,
}