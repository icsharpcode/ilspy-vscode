/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

// from https://raw.githubusercontent.com/jbevain/cecil/505b07d6974d8405a63124139733c6fdc0e67bc7/Mono.Cecil.Metadata/TokenType.cs
export enum TokenType
{
		Module = 0x00000000,
		TypeRef = 0x01000000,
		TypeDef = 0x02000000,
		Field = 0x04000000,
		Method = 0x06000000,
		Param = 0x08000000,
		InterfaceImpl = 0x09000000,
		MemberRef = 0x0a000000,
		CustomAttribute = 0x0c000000,
		Permission = 0x0e000000,
		Signature = 0x11000000,
		Event = 0x14000000,
		Property = 0x17000000,
		ModuleRef = 0x1a000000,
		TypeSpec = 0x1b000000,
		Assembly = 0x20000000,
		AssemblyRef = 0x23000000,
		File = 0x26000000,
		ExportedType = 0x27000000,
		ManifestResource = 0x28000000,
		GenericParam = 0x2a000000,
		MethodSpec = 0x2b000000,
		GenericParamConstraint = 0x2c000000,

		Document = 0x30000000,
		MethodDebugInformation = 0x31000000,
		LocalScope = 0x32000000,
		LocalVariable = 0x33000000,
		LocalConstant = 0x34000000,
		ImportScope = 0x35000000,
		StateMachineMethod = 0x36000000,
		CustomDebugInformation = 0x37000000,

        String = 0x70000000,
}

