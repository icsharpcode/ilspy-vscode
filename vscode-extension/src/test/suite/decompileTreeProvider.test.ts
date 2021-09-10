/*---------------------------------------------------------------------------------------------
 *  Copyright (c) Microsoft Corporation. All rights reserved.
 *  Licensed under the MIT License. See License.txt in the project root for license information.
 *--------------------------------------------------------------------------------------------*/

import { expect } from "chai";
import { DecompiledTreeProvider } from "../../decompiler/DecompiledTreeProvider";
import { TreeItemCollapsibleState } from "vscode";
import MockILSpyBackend from "./MockILSpyBackend";
import { MemberSubKind } from "../../decompiler/MemberSubKind";
import { TokenType } from "../../decompiler/TokenType";
import { MemberNode } from "../../decompiler/MemberNode";

suite("Member Node Tests", () => {
  test("Assembly node returns true for isType", () => {
    const node = new MemberNode(
      "assembly",
      "name",
      0,
      TokenType.AssemblyDefinition,
      MemberSubKind.None,
      -1
    );

    expect(node.mayHaveChildren).to.be.a("boolean").that.equal(true);
  });

  test("Type node returns true for isType", () => {
    const node = new MemberNode(
      "assembly",
      "name",
      0,
      TokenType.TypeDefinition,
      MemberSubKind.Class,
      0
    );

    expect(node.mayHaveChildren).to.be.a("boolean").that.equal(true);
  });

  test("Other nodes return false for isType", () => {
    const node = new MemberNode(
      "assembly",
      "name",
      0,
      TokenType.MemberReference,
      MemberSubKind.None,
      0
    );

    expect(node.mayHaveChildren).to.be.a("boolean").that.equal(false);
  });
});

interface IconPath {
  light: string;
  dark: string;
}

suite("Tree Data Provider tests", () => {
  test("Assembly node should be collapsible", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.AssemblyDefinition,
        MemberSubKind.None,
        -1
      )
    );

    expect(treeItem.collapsibleState).to.not.equal(
      TreeItemCollapsibleState.None
    );
    expect(treeItem.collapsibleState).to.not.equal(undefined);
  });

  test("Type node should be collapsible", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.TypeDefinition,
        MemberSubKind.Class,
        0
      )
    );

    expect(treeItem.collapsibleState).to.not.equal(
      TreeItemCollapsibleState.None
    );
    expect(treeItem.collapsibleState).to.not.equal(undefined);
  });

  test("Other node should not be collapsible", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.MemberReference,
        MemberSubKind.None,
        0
      )
    );

    expect(treeItem.collapsibleState).to.equal(undefined);
  });

  test("Class node should have Class Icon", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.TypeDefinition,
        MemberSubKind.Class,
        0
      )
    );
    const iconPath = treeItem.iconPath;

    expect(iconPath).to.be.a("Object");
    expect(iconPath).to.have.property("light");
    expect(iconPath).to.have.property("dark");
    expect((<IconPath>iconPath).light).to.be.a("string");
    expect((<IconPath>iconPath).dark).to.be.a("string");
    expect(<string>(<IconPath>iconPath).light).to.include("Class_16x");
    expect(<string>(<IconPath>iconPath).dark).to.include("Class_inverse_16x");
  });

  test("Interface node should have Interface Icon", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.TypeDefinition,
        MemberSubKind.Interface,
        0
      )
    );
    const iconPath = treeItem.iconPath;

    expect(iconPath).to.be.a("Object");
    expect(iconPath).to.have.property("light");
    expect(iconPath).to.have.property("dark");
    expect((<IconPath>iconPath).light).to.be.a("string");
    expect((<IconPath>iconPath).dark).to.be.a("string");
    expect(<string>(<IconPath>iconPath).light).to.include("Interface_16x");
    expect(<string>(<IconPath>iconPath).dark).to.include(
      "Interface_inverse_16x"
    );
  });

  test("Structure node should have Structure Icon", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.TypeDefinition,
        MemberSubKind.Struct,
        0
      )
    );
    const iconPath = treeItem.iconPath;

    expect(iconPath).to.be.a("Object");
    expect(iconPath).to.have.property("light");
    expect(iconPath).to.have.property("dark");
    expect((<IconPath>iconPath).light).to.be.a("string");
    expect((<IconPath>iconPath).dark).to.be.a("string");
    expect(<string>(<IconPath>iconPath).light).to.include("Structure_16x");
    expect(<string>(<IconPath>iconPath).dark).to.include(
      "Structure_inverse_16x"
    );
  });

  test("Enum node should have Enum Icon", () => {
    const provider = new DecompiledTreeProvider(new MockILSpyBackend());
    const treeItem = provider.getTreeItem(
      new MemberNode(
        "assembly",
        "name",
        0,
        TokenType.TypeDefinition,
        MemberSubKind.Enum,
        0
      )
    );
    const iconPath = treeItem.iconPath;

    expect(iconPath).to.be.a("Object");
    expect(iconPath).to.have.property("light");
    expect(iconPath).to.have.property("dark");
    expect((<IconPath>iconPath).light).to.be.a("string");
    expect((<IconPath>iconPath).dark).to.be.a("string");
    expect(<string>(<IconPath>iconPath).light).to.include("EnumItem_16x");
    expect(<string>(<IconPath>iconPath).dark).to.include(
      "EnumItem_inverse_16x"
    );
  });
});
