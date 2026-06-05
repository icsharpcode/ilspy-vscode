/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { ThemeIcon, Uri } from "vscode";
import { NodeType } from "./extension-types";
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
const ProductColorMapping: {
  [key in NodeType]?: { id: string };
} = {
  [NodeType.Assembly]: { id: "symbolIcon.packageForeground" },
  [NodeType.Namespace]: { id: "symbolIcon.namespaceForeground" },
  [NodeType.Event]: { id: "symbolIcon.eventForeground" },
  [NodeType.Field]: { id: "symbolIcon.fieldForeground" },
  [NodeType.Method]: { id: "symbolIcon.methodForeground" },
  [NodeType.Enum]: { id: "symbolIcon.enumeratorForeground" },
  [NodeType.Class]: { id: "symbolIcon.classForeground" },
  [NodeType.Interface]: { id: "symbolIcon.interfaceForeground" },
  [NodeType.Struct]: { id: "symbolIcon.structForeground" },
  [NodeType.Delegate]: { id: "symbolIcon.classForeground" },
  [NodeType.Const]: { id: "symbolIcon.constantForeground" },
  [NodeType.Property]: { id: "symbolIcon.propertyForeground" },
  [NodeType.ReferencesRoot]: { id: "symbolIcon.folderForeground" },
  [NodeType.AssemblyReference]: { id: "symbolIcon.referenceForeground" },
  [NodeType.Unknown]: { id: "foreground" },
  [NodeType.Analyzer]: { id: "symbolIcon.methodForeground" },
  [NodeType.BaseTypes]: { id: "symbolIcon.interfaceForeground" },
  [NodeType.DerivedTypes]: { id: "symbolIcon.interfaceForeground" },
  [NodeType.NuGetPackage]: { id: "symbolIcon.referenceForeground" },
  [NodeType.PackageFolder]: { id: "symbolIcon.folderForeground" },
  [NodeType.Resource]: { id: "symbolIcon.fileForeground" },
};

export function getNodeIcon(nodeType: NodeType | undefined) {
  const iconMapping =
    ProductIconMapping[nodeType ?? NodeType.Unknown] ?? UNKNOWN_ICON;
  const colorMapping =
    ProductColorMapping[nodeType ?? NodeType.Unknown] ?? undefined;
  if ("id" in iconMapping) {
    return new ThemeIcon(iconMapping.id, colorMapping);
  } else {
    const basePath = path.join(
      __dirname,
      "..",
      "resources",
      "tree-icons",
      iconMapping.customIcon,
    );
    return {
      light: Uri.file(`${basePath}_light.svg`),
      dark: Uri.file(`${basePath}_dark.svg`),
    };
  }
}
