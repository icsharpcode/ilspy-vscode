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
import { AddAssemblyRequest, ListNamespacesRequest, ListTypesRequest, ListMembersRequest, DecompileAssemblyRequest, DecompileTypeRequest, DecompileMemberRequest } from './protocol';

export class MemberNode {
    private _decompiled: string = null;

    constructor(
        private _assembly: string,
        private _name: string,
        private _rid: number,
        private _tokenType: TokenType,
        private _typeDefSubKind: MemberSubKind,
        private _parentRid : number) {
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
        return this.type === TokenType.TypeDef
         || this.type === TokenType.Assembly
         || this.type === TokenType.Namespace;
    }

    public get parent(): number {
        return this._parentRid;
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
        let escaped: string = assembly.replace(/\\/g, "\\\\",);
        let request: AddAssemblyRequest = { "AssemblyPath": escaped };
        this.server.assemblyPaths.add(escaped);
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
            case TokenType.Assembly:
                name = "Document";
                break;
            case TokenType.Namespace:
                name = "Namespace";
                break;
            case TokenType.Event:
                name = "Event";
                break;
            case TokenType.Field:
                name = "Field";
                break;
            case TokenType.Method:
                name = "Method";
                break;
            case TokenType.TypeDef:
                switch (node.memberSubKind)
                {
                    case MemberSubKind.Enum:
                        name = "EnumItem";
                        break;
                    case MemberSubKind.Interface:
                        name = "Interface";
                        break;
                    case MemberSubKind.Structure:
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
            case TokenType.Property:
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
                result.push(new MemberNode(e, e, -2, TokenType.Assembly, MemberSubKind.None, -3));
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
            return result.Namespaces.map(n => new MemberNode(assembly, n, -1, TokenType.Namespace, MemberSubKind.None, -2));
        });
    }

    getTypes(assembly: string, namespace: string): Thenable<MemberNode[]> {
        let request: ListTypesRequest = { "AssemblyPath": assembly, "Namespace": namespace };
        return serverUtils.getTypes(this.server, request).then(result => {
            return result.Types.map(t => new MemberNode(assembly, t.Name, t.Token.RID, t.Token.TokenType, t.MemberSubKind, -1));
        });
    }

    getMembers(element: MemberNode): Thenable<MemberNode[]> {
        let request: ListMembersRequest = {"AssemblyPath": element.assembly, "Rid": element.rid };
        if (element.mayHaveChildren) {
            return serverUtils.getMembers(this.server, request).then(result => {
                return result.Members.map(m => new MemberNode(element.assembly, m.Name, m.Token.RID, m.Token.TokenType, m.MemberSubKind, element.rid));
            });
        }
        else {
            return MemberNode[0];
        }
    }

    public getCode(element?: MemberNode): Thenable<string> {
        if (element.rid === -2) {
            let request: DecompileAssemblyRequest = { "AssemblyPath": element.assembly };
            return serverUtils.decompileAssembly(this.server, request).then(result => result.Decompiled);
        }

        if (element.rid === -1) {
            let name = element.name.length == 0 ? "<global>" : element.name;
            return Promise.resolve("namespace " + name + " { }");
        }

        if (element.mayHaveChildren) {
            let request: DecompileTypeRequest = {"AssemblyPath": element.assembly, "Rid": element.rid};
            return serverUtils.decompileType(this.server, request).then(result => result.Decompiled);
        }
        else {
            let request: DecompileMemberRequest = {"AssemblyPath": element.assembly, "TypeRid": element.parent, "MemberType": element.type, "MemberRid": element.rid};
            return serverUtils.decompileMember(this.server, request).then(result => result.Decompiled);
        }
    }

	public provideTextDocumentContent(uri: Uri, token: CancellationToken): ProviderResult<string> {
        //TODO:
		return "";
	}
}