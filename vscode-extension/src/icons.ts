/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { ThemeIcon } from "vscode";
import { NodeType } from "./protocol/NodeType";
import * as path from "path";

const UNKNOWN_ICON = { id: "question" };

const ProductIconMapping: {
  [key in NodeType]?: { id: string } | { customIcon: string };
} = {
  [NodeType.Assembly]: { id: "library" },
  [NodeType.Namespace]: { id: "symbol-namespace" },
  [NodeType.Event]: { id: "symbol-event" },
  [NodeType.Field]: { id: "symbol-field" },
  [NodeType.Method]: { id: "symbol-method" },
  [NodeType.Enum]: { id: "symbol-enum" },
  [NodeType.Class]: { id: "symbol-class" },
  [NodeType.Interface]: { id: "symbol-interface" },
  [NodeType.Struct]: { id: "symbol-struct" },
  [NodeType.Delegate]: { id: "symbol-class" },
  [NodeType.Const]: { id: "symbol-constant" },
  [NodeType.Property]: { id: "symbol-property" },
  [NodeType.ReferencesRoot]: { id: "folder-library" },
  [NodeType.AssemblyReference]: { id: "library" },
  [NodeType.Unknown]: UNKNOWN_ICON,
  [NodeType.Analyzer]: { id: "question" },
  [NodeType.BaseTypes]: { id: "arrow-up" },
  [NodeType.DerivedTypes]: { id: "arrow-down" },
  [NodeType.NuGetPackage]: { customIcon: "nuget" },
  [NodeType.PackageFolder]: { id: "folder" },
  [NodeType.Resource]: { id: "file" },
};

export function getNodeIcon(nodeType: NodeType | undefined) {
  const iconMapping =
    ProductIconMapping[nodeType ?? NodeType.Unknown] ?? UNKNOWN_ICON;
  if ("id" in iconMapping) {
    return new ThemeIcon(iconMapping.id);
  } else {
    const basePath = path.join(
      __dirname,
      "..",
      "resources",
      "tree-icons",
      iconMapping.customIcon
    );
    return { light: `${basePath}_light.svg`, dark: `${basePath}_dark.svg` };
  }
}
