import { TreeItemCollapsibleState } from 'vscode';
import { expect, should } from 'chai';
import { MemberNode, DecompiledTreeProvider } from '../src/msildecompiler/decompiledTreeProvider';
import { TokenType } from '../src/msildecompiler/tokenType';

suite("Member Node Tests", () => {
    test("Assembly node returns true for isType", () => {
        const node = new MemberNode("assembly", "name", 0, TokenType.Assembly, -1);

        expect(node.isTypeDefOrAssembly).to.be.a('boolean');
        expect(node.isTypeDefOrAssembly).to.equal(true);
    });

    test("Type node returns true for isType", () => {
        const node = new MemberNode("assembly", "name", 0, TokenType.TypeDef, 0);

        expect(node.isTypeDefOrAssembly).to.be.a('boolean');
        expect(node.isTypeDefOrAssembly).to.equal(true);
    });


    test("Other nodes return false for isType", () => {
        const node = new MemberNode("assembly", "name", 0, TokenType.MemberRef, 0);

        expect(node.isTypeDefOrAssembly).to.be.a('boolean');
        expect(node.isTypeDefOrAssembly).to.equal(false);
    });
});

suite("Tree Data Provider tests", () => {
    test("Assembly node should be collapsible", () =>{
        const provider = new DecompiledTreeProvider(null);
        const treeItem = provider.getTreeItem(new MemberNode("assembly", "name", 0, TokenType.Assembly, -1));

        expect(treeItem.collapsibleState).to.not.equal(TreeItemCollapsibleState.None);
        expect(treeItem.collapsibleState).to.not.equal(undefined);
    });

    test("Type node should be collapsible", () =>{
        const provider = new DecompiledTreeProvider(null);
        const treeItem = provider.getTreeItem(new MemberNode("assembly", "name", 0, TokenType.TypeDef, 0));

        expect(treeItem.collapsibleState).to.not.equal(TreeItemCollapsibleState.None);
        expect(treeItem.collapsibleState).to.not.equal(undefined);
    });

    test("Other node should not be collapsible", () =>{
        const provider = new DecompiledTreeProvider(null);
        const treeItem = provider.getTreeItem(new MemberNode("assembly", "name", 0, TokenType.MemberRef, 0));

        expect(treeItem.collapsibleState).to.equal(undefined);
    });
});