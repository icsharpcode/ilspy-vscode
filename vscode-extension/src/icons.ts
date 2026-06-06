/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2022 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import { ThemeColor, ThemeIcon, Uri } from "vscode";
import { NodeType } from "./extension-types";
import * as path from "path";

const UNKNOWN_ICON = { id: "question", colorKey: "foreground" };

const ProductIconMapping: {
  [key in NodeType]?: { id: string; colorKey: string } | { customIcon: string };
} = {
  [NodeType.Assembly]: {
    id: "library",
    colorKey: "symbolIcon.packageForeground",
  },
  [NodeType.Namespace]: {
    id: "symbol-namespace",
    colorKey: "symbolIcon.namespaceForeground",
  },
  [NodeType.Event]: {
    id: "symbol-event",
    colorKey: "symbolIcon.eventForeground",
  },
  [NodeType.Field]: {
    id: "symbol-field",
    colorKey: "symbolIcon.fieldForeground",
  },
  [NodeType.Method]: {
    id: "symbol-method",
    colorKey: "symbolIcon.methodForeground",
  },
  [NodeType.Enum]: {
    id: "symbol-enum",
    colorKey: "symbolIcon.enumeratorForeground",
  },
  [NodeType.Class]: {
    id: "symbol-class",
    colorKey: "symbolIcon.classForeground",
  },
  [NodeType.Interface]: {
    id: "symbol-interface",
    colorKey: "symbolIcon.interfaceForeground",
  },
  [NodeType.Struct]: {
    id: "symbol-struct",
    colorKey: "symbolIcon.structForeground",
  },
  [NodeType.Delegate]: {
    id: "symbol-class",
    colorKey: "symbolIcon.classForeground",
  },
  [NodeType.Const]: {
    id: "symbol-constant",
    colorKey: "symbolIcon.constantForeground",
  },
  [NodeType.Property]: {
    id: "symbol-property",
    colorKey: "symbolIcon.propertyForeground",
  },
  [NodeType.ReferencesRoot]: {
    id: "folder-library",
    colorKey: "symbolIcon.folderForeground",
  },
  [NodeType.AssemblyReference]: {
    id: "library",
    colorKey: "symbolIcon.referenceForeground",
  },
  [NodeType.Unknown]: UNKNOWN_ICON,
  [NodeType.Analyzer]: {
    id: "question",
    colorKey: "symbolIcon.methodForeground",
  },
  [NodeType.BaseTypes]: {
    id: "arrow-up",
    colorKey: "symbolIcon.interfaceForeground",
  },
  [NodeType.DerivedTypes]: {
    id: "arrow-down",
    colorKey: "symbolIcon.interfaceForeground",
  },
  [NodeType.NuGetPackage]: { customIcon: "nuget" },
  [NodeType.PackageFolder]: {
    id: "folder",
    colorKey: "symbolIcon.folderForeground",
  },
  [NodeType.Resource]: { id: "file", colorKey: "symbolIcon.fileForeground" },
};

export function getNodeIcon(nodeType: NodeType | undefined) {
  const iconMapping =
    ProductIconMapping[nodeType ?? NodeType.Unknown] ?? UNKNOWN_ICON;
  if ("id" in iconMapping) {
    return new ThemeIcon(iconMapping.id, new ThemeColor(iconMapping.colorKey));
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
