import { ExtensionContext, TreeDataProvider, EventEmitter, TreeItem, Event, window, TreeItemCollapsibleState, Uri, commands, workspace, TextDocumentContentProvider, CancellationToken, ProviderResult } from 'vscode';
import { MsilDecompilerServer } from './server';
import { TokenType } from './tokenType';
import * as serverUtils from './utils';
import * as protocol from './protocol';
import * as path from 'path';

export class MemberNode {
    private _decompiled: string = null;

    constructor(private _name: string, private _rid: number, private _tokenType: TokenType, private _parentRid : number) {
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

    public get isType() : boolean {
        return this.type === TokenType.TypeDef
         || this.type === TokenType.Assembly;
    }

    public get parent(): number {
        return this._parentRid;
    }
}

export class DecompiledTreeProvider implements TreeDataProvider<MemberNode>, TextDocumentContentProvider {
	private _onDidChangeTreeData: EventEmitter<any> = new EventEmitter<any>();
	readonly onDidChangeTreeData: Event<any> = this._onDidChangeTreeData.event;

    constructor(private server: MsilDecompilerServer) {
    }

	public refresh(): void {
		this._onDidChangeTreeData.fire();
	}

    public getTreeItem(element: MemberNode): TreeItem {
        return {
            label: element.name,
            collapsibleState: element.isType ? TreeItemCollapsibleState.Collapsed : void 0,
            command: {
				command: 'showDecompiledCode',
				arguments: [element],
				title: 'Decompile'
			},
            iconPath: {
				light: element.isType ? path.join(__filename, '..', '..', '..', 'resources', 'light', 'folder.svg') : path.join(__filename, '..', '..', '..', 'resources', 'light', 'document.svg'),
				dark: element.isType ? path.join(__filename, '..', '..', '..', 'resources', 'dark', 'folder.svg') : path.join(__filename, '..', '..', '..', 'resources', 'dark', 'document.svg')
			}
        };
    }

	public getChildren(element?: MemberNode): MemberNode[] | Thenable<MemberNode[]> {
		if (!element) {
            return [
                new MemberNode(this.server.assemblyPath, -1, TokenType.Assembly, -2)
            ];
		}
        else if (element.rid === -1) {
            return this.getTypes();
        } else {
            return this.getMembers(element);
        }
	}

    getTypes(): Thenable<MemberNode[]> {
        return serverUtils.getTypes(this.server, { }).then(result => {
            return result.Types.map(t => new MemberNode(t.Name, t.Token.RID, t.Token.TokenType, -1));
        });
    }

    getMembers(element: MemberNode): Thenable<MemberNode[]> {
        if (element.isType) {
            return serverUtils.getMembers(this.server, { "Rid": element.rid }).then(result => {
                return result.Members.map(m => new MemberNode(m.Name, m.Token.RID, m.Token.TokenType, element.rid));
            });
        }
        else {
            return MemberNode[0];
        }
    }

    public getCode(element?: MemberNode): Thenable<string> {
        if (element.rid === -1) {
            return serverUtils.decompileAssembly(this.server, { }).then(result => result.Decompiled);
        }

        if (element.isType) {
            return serverUtils.decompileType(this.server, {"Rid": element.rid}).then(result => result.Decompiled);
        }
        else {
            return serverUtils.decompileMember(this.server, {"TypeRid": element.parent, "MemberRid": element.rid}).then(result => result.Decompiled);
        }
    }

	public provideTextDocumentContent(uri: Uri, token: CancellationToken): ProviderResult<string> {
        //TODO:
		return "";
	}
}