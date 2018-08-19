/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { TreeDataProvider, EventEmitter, TreeItem, Event, TreeItemCollapsibleState, Uri, TextDocumentContentProvider, CancellationToken, ProviderResult } from 'vscode';
import { MsilDecompilerServer } from './server';
import { TokenType } from './tokenType';
import { MemberSubKind } from './memberSubKind';
import * as serverUtils from './utils';
import * as path from 'path';
import { AddAssemblyRequest, ListNamespacesRequest, ListTypesRequest, ListMembersRequest, DecompileAssemblyRequest, DecompileTypeRequest, DecompileMemberRequest, MemberData } from './protocol';

export class LangaugeNames {
    public static readonly CSharp = "CSharp";
    public static readonly IL = "IL";
}

export class MemberNode {
    private _decompiled: string = null;

    constructor(
        private _assembly: string,
        private _name: string,
        private _rid: number,
        private _tokenType: TokenType,
        private _typeDefSubKind: MemberSubKind,
        private _parentToken : number) {
    }

    public get name(): string {
        return this._name;
    }

    public get rid(): number {
        return this._rid;
    }

    public get type(): TokenType {
        return this._tokenType;
    }

    public get decompiled(): string {
        return this._decompiled;
    }

    public set decompiled(val: string) {
        this._decompiled = val;
    }

    public get mayHaveChildren() : boolean {
        return this.type === TokenType.TypeDefinition
         || this.type === TokenType.AssemblyDefinition
         || this.type === TokenType.NamespaceDefinition;
    }

    public get parent(): number {
        return this._parentToken;
    }

    public get assembly(): string {
        return this._assembly;
    }

    public get memberSubKind(): MemberSubKind {
        return this._typeDefSubKind;
    }
}


 interface ThenableTreeIconPath {
    light: string;
    dark: string;
}

export class DecompiledTreeProvider implements TreeDataProvider<MemberNode>, TextDocumentContentProvider {
    private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
    readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

    constructor(private server: MsilDecompilerServer) {
    }

    public refresh(): void {
        this._onDidChangeTreeData.fire();
    }

    public addAssembly(assembly: string): Thenable<boolean> {
        let request: AddAssemblyRequest = { "AssemblyPath": assembly };
        this.server.assemblyPaths.add(assembly);
        return serverUtils.addAssembly(this.server, request).then(response => {
            return response.Added;
        });
    }

    public getTreeItem(element: MemberNode): TreeItem {
        return {
            label: element.name,
            collapsibleState: element.mayHaveChildren ? TreeItemCollapsibleState.Collapsed : void 0,
            command: {
                command: 'showDecompiledCode',
                arguments: [element],
                title: 'Decompile'
            },
            iconPath: this.getIconByTokenType(element)
        };
    }

    getIconByTokenType(node: MemberNode): ThenableTreeIconPath {
        let name: string = null;

        switch (node.type) {
            case TokenType.AssemblyDefinition:
                name = "Document";
                break;
            case TokenType.NamespaceDefinition:
                name = "Namespace";
                break;
            case TokenType.EventDefinition:
                name = "Event";
                break;
            case TokenType.FieldDefinition:
                name = "Field";
                break;
            case TokenType.MethodDefinition:
                name = "Method";
                break;
            case TokenType.TypeDefinition:
                switch (node.memberSubKind)
                {
                    case MemberSubKind.Enum:
                        name = "EnumItem";
                        break;
                    case MemberSubKind.Interface:
                        name = "Interface";
                        break;
                    case MemberSubKind.Struct:
                        name = "Structure";
                        break;
                    default:
                        name = "Class";
                        break;
                }
                break;
            case TokenType.LocalConstant:
                name = "Constant";
                break;
            case TokenType.PropertyDefinition:
                name = "Property";
                break;
            default:
                name = "Misc";
                break;
        }

        let normalName = name + "_16x.svg";
        let inverseName = name + "_inverse_16x.svg";
        let lightIconPath = path.join(__dirname, '..', '..', '..', 'resources', normalName);
        let darkIconPath = path.join(__dirname, '..', '..', '..', 'resources', inverseName);

        return {
            light: lightIconPath,
            dark: darkIconPath
        };
    }

    public getChildren(element?: MemberNode): MemberNode[] | Thenable<MemberNode[]> {
        if (this.server.assemblyPaths.size <= 0) {
             return [];
        }

        // Nothing yet so add assembly nodes
        if (!element) {
            let result = [];
            for (let e of this.server.assemblyPaths) {
                result.push(new MemberNode(e, e, -2, TokenType.AssemblyDefinition, MemberSubKind.None, -3));
            }

            return result;
        }
        else if (element.rid === -2) {
            return this.getNamespaces(element.assembly);
        }
        else if (element.rid === -1) {
            return this.getTypes(element.assembly, element.name);
        } else {
            return this.getMembers(element);
        }
    }

    getNamespaces(assembly: string): Thenable<MemberNode[]> {
        let request: ListNamespacesRequest = { "AssemblyPath": assembly };
        return serverUtils.listNamespaces(this.server, request).then(result => {
            return result.Namespaces.map(n => new MemberNode(assembly, n, -1, TokenType.NamespaceDefinition, MemberSubKind.None, -2));
        });
    }

    getTypes(assembly: string, namespace: string): Thenable<MemberNode[]> {
        let request: ListTypesRequest = { "AssemblyPath": assembly, "Namespace": namespace };
        return serverUtils.getTypes(this.server, request).then(result => {
            return result.Types.map(t => new MemberNode(assembly, t.Name, this.getRid(t), this.getHandleKind(t), t.MemberSubKind, -1));
        });
    }

    getMembers(element: MemberNode): Thenable<MemberNode[]> {
        let request: ListMembersRequest = {"AssemblyPath": element.assembly, "Handle": this.makeHandle(element) };
        if (element.mayHaveChildren) {
            return serverUtils.getMembers(this.server, request).then(result => {
                return result.Members.map(m => new MemberNode(element.assembly, m.Name, this.getRid(m), this.getHandleKind(m), m.MemberSubKind, element.rid));
            });
        }
        else {
            return MemberNode[0];
        }
    }

    public getCode(element?: MemberNode): Thenable<string> {
        if (element.rid === -2) {
            let request: DecompileAssemblyRequest = { "AssemblyPath": element.assembly };
            return serverUtils.decompileAssembly(this.server, request).then(result => result.Decompiled[LangaugeNames.CSharp]);
        }

        if (element.rid === -1) {
            let name = element.name.length == 0 ? "<global>" : element.name;
            return Promise.resolve("namespace " + name + " { }");
        }

        if (element.mayHaveChildren) {
            let request: DecompileTypeRequest = {"AssemblyPath": element.assembly, "Handle": this.makeHandle(element)};
            return serverUtils.decompileType(this.server, request).then(result => result.Decompiled[LangaugeNames.CSharp]);
        }
        else {
            let request: DecompileMemberRequest = {"AssemblyPath": element.assembly, "Type": element.parent, "Member": this.makeHandle(element)};
            return serverUtils.decompileMember(this.server, request).then(result => result.Decompiled[LangaugeNames.CSharp]);
        }
    }

    public provideTextDocumentContent(uri: Uri, token: CancellationToken): ProviderResult<string> {
        //TODO:
        return "";
    }

    // metadata tokens/handles are 32-bit unsigned integers in the format:
    // the first byte is the handle kind/token type, the other three bytes are used for the row-id.
    makeHandle(element: MemberNode): number {
        return (element.type << 24) | element.rid;
    }

    // extract the row-id by removing the first byte
    getRid(member: MemberData): number {
        return member.Token & 0x00FFFFFF;
    }

    // extract the token/handle kind by shifting the first byte to the position of the first byte
    // apply bit-and 0xFF to the result to ensure that the other bytes are zero.
    getHandleKind(member: MemberData): number {
        return (member.Token >> 24) & 0xFF;
    }
}