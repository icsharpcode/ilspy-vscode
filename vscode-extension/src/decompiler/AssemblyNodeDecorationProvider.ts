import {
  FileDecoration,
  FileDecorationProvider,
  ThemeColor,
  Uri,
} from "vscode";

export class AssemblyNodeDecorationProvider implements FileDecorationProvider {
  async provideFileDecoration(uri: Uri): Promise<FileDecoration | undefined> {
    if (uri.scheme === "ilspy-autoloaded") {
      return {
        color: new ThemeColor("ilspy.autoLoadedAssemblyTextForeground"),
      };
    }

    return undefined;
  }
}
