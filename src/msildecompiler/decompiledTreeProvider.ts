import { TreeDataProvider, EventEmitter, TreeItem, Event, TreeItemCollapsibleState, Uri, TextDocumentContentProvider, CancellationToken, ProviderResult } from 'vscode';
import { MsilDecompilerServer } from './server';
import { TokenType } from './tokenType';
import * as serverUtils from './utils';
import * as path from 'path';
import * as protocol from './protocol';

export class MemberNode {
    private _decompiled: string = null;

    constructor(private _assembly, private _name: string, private _rid: number, private _tokenType: TokenType, private _parentRid : number) {
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

    public get isTypeDefOrAssembly() : boolean {
        return this.type === TokenType.TypeDef
         || this.type === TokenType.Assembly;
    }

    public get parent(): number {
        return this._parentRid;
    }

    public get assembly(): string {
        return this._assembly;
    }
}


 interface ThemableTreeIconPath {
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
        this.server.assemblyPaths.add(escaped);
        return serverUtils.addAssembly(this.server, { "AssemblyPath": escaped }).then(response => {
            return response.Added;
        });
    }

    public getTreeItem(element: MemberNode): TreeItem {
        return {
            label: element.name,
            collapsibleState: element.isTypeDefOrAssembly ? TreeItemCollapsibleState.Collapsed : void 0,
            command: {
				command: 'showDecompiledCode',
				arguments: [element],
				title: 'Decompile'
			},
            iconPath: this.getIconByTokenType(element.type)
        };
    }

    getIconByTokenType(type: TokenType): ThemableTreeIconPath {
        let name: string = null;

        switch (type) {
            case TokenType.Assembly:
                name = "Document";
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
                name = "Class";
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
                result.push(new MemberNode(e, e, -1, TokenType.Assembly, -2));
            }

            return result;
		}
        else if (element.rid === -1) {
            return this.getTypes(element.assembly);
        } else {
            return this.getMembers(element);
        }
	}

    getTypes(assembly: string): Thenable<MemberNode[]> {
        return serverUtils.getTypes(this.server, { "AssemblyPath": assembly }).then(result => {
            return result.Types.map(t => new MemberNode(assembly, t.Name, t.Token.RID, t.Token.TokenType, -1));
        });
    }

    getMembers(element: MemberNode): Thenable<MemberNode[]> {
        if (element.isTypeDefOrAssembly) {
            return serverUtils.getMembers(this.server, {"AssemblyPath": element.assembly, "Rid": element.rid }).then(result => {
                return result.Members.map(m => new MemberNode(element.assembly, m.Name, m.Token.RID, m.Token.TokenType, element.rid));
            });
        }
        else {
            return MemberNode[0];
        }
    }

    public getCode(element?: MemberNode): Thenable<string> {
        if (element.rid === -1) {
            return serverUtils.decompileAssembly(this.server, { "AssemblyPath": element.assembly }).then(result => result.Decompiled);
        }

        if (element.isTypeDefOrAssembly) {
            return serverUtils.decompileType(this.server, {"AssemblyPath": element.assembly, "Rid": element.rid}).then(result => result.Decompiled);
        }
        else {
            return serverUtils.decompileMember(this.server, {"AssemblyPath": element.assembly, "TypeRid": element.parent, "MemberType": element.type, "MemberRid": element.rid}).then(result => result.Decompiled);
        }
    }

	public provideTextDocumentContent(uri: Uri, token: CancellationToken): ProviderResult<string> {
        //TODO:
		return "";
	}
}