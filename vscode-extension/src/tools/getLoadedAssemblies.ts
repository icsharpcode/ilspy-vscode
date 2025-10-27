import * as vscode from "vscode";
import { DecompiledTreeProvider } from "../decompiler/DecompiledTreeProvider";
import { nodeDataToUri } from "../decompiler/nodeUri";
import Node from "../protocol/Node";

export interface ListLoadedAssembliesToolInput {}

/**
 * Tool: List Loaded Assemblies
 * List all currently loaded assemblies
 */
export function registerListLoadedAssembliesTool(
	treeProvider: DecompiledTreeProvider
): vscode.Disposable {
	return vscode.lm.registerTool<ListLoadedAssembliesToolInput>("ilspy_list_loaded_assemblies", {
		async prepareInvocation(
			_options,
			_token
		) {
			return {
				invocationMessage: new vscode.MarkdownString("$(library) Getting loaded assemblies..."),
			};
		},

		async invoke(
			_options,
			_token
		) {
			try {
				const nodes = (await treeProvider.getChildren()) as Node[];
				const assemblies = nodes.filter((node) => node.metadata);

				if (assemblies.length === 0) {
					return new vscode.LanguageModelToolResult([
						new vscode.LanguageModelTextPart("No assemblies loaded."),
					]);
				}

				const assemblyData = assemblies.map((node) => ({
					name: node.metadata!.name,
					path: node.metadata!.assemblyPath,
					uri: nodeDataToUri(node).toString(),
				}));

				return new vscode.LanguageModelToolResult([
					new vscode.LanguageModelTextPart(
						JSON.stringify({ count: assemblies.length, assemblies: assemblyData }, null, 2)
					),
				]);
			} catch (error) {
				const errorMessage = error instanceof Error ? error.message : String(error);
				throw new Error(`Failed to get loaded assemblies: ${errorMessage}`);
			}
		}
	});
}
