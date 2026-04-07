/*------------------------------------------------------------------------------------------------
 *  Copyright (c) 2026 ICSharpCode
 *  Licensed under the MIT License. See LICENSE.TXT in the project root for license information.
 *-----------------------------------------------------------------------------------------------*/

import * as nodePath from "path";
import * as vscode from "vscode";
import IILSpyBackend from "../decompiler/IILSpyBackend";
import { AvailableNodeCommands } from "../protocol/AvailableNodeCommands";
import AssemblyData from "../protocol/AssemblyData";
import Node from "../protocol/Node";
import NodeMetadata from "../protocol/NodeMetadata";
import { NodeType } from "../protocol/NodeType";

export interface AssemblyFilterInput {
  assemblyPath?: string;
  assemblyName?: string;
}

export interface SymbolQueryInput extends AssemblyFilterInput {
  symbol?: string;
}

export function createJsonResult(
  data: unknown,
): vscode.LanguageModelToolResult {
  return new vscode.LanguageModelToolResult([
    new vscode.LanguageModelTextPart(JSON.stringify(data, null, 2)),
  ]);
}

export async function refreshAssemblyList() {
  await vscode.commands.executeCommand("ilspy.refreshAssemblyList");
}

export async function getRootNodes(backend: IILSpyBackend): Promise<Node[]> {
  const response = await backend.sendGetNodes({});
  if (response?.shouldUpdateAssemblyList) {
    await refreshAssemblyList();
  }

  return response?.nodes ?? [];
}

export async function getAssemblyNodes(
  backend: IILSpyBackend,
): Promise<Node[]> {
  return (await getRootNodes(backend)).filter(
    (node) => node.metadata?.type === NodeType.Assembly,
  );
}

export async function searchNodes(
  backend: IILSpyBackend,
  term: string,
  filter?: AssemblyFilterInput,
): Promise<Node[]> {
  const response = await backend.sendSearch({ term });
  if (response?.shouldUpdateAssemblyList) {
    await refreshAssemblyList();
  }

  return (response?.results ?? []).filter((node) =>
    matchesAssembly(node, filter),
  );
}

export async function resolveAssemblyNode(
  backend: IILSpyBackend,
  filter: AssemblyFilterInput,
): Promise<Node> {
  if (!hasAssemblyFilter(filter)) {
    throw new Error("An assemblyPath or assemblyName is required.");
  }

  const matches = (await getAssemblyNodes(backend)).filter((node) =>
    matchesAssembly(node, filter),
  );

  if (matches.length === 1) {
    return matches[0];
  }

  if (matches.length === 0) {
    throw new Error(
      `No loaded assembly matched ${describeAssemblyFilter(filter)}.`,
    );
  }

  throw new Error(
    `Multiple loaded assemblies matched ${describeAssemblyFilter(filter)}. ` +
      `Use an exact assemblyPath. Candidates: ${matches
        .slice(0, 10)
        .map((node) => summarizeAssemblyNode(node).assemblyPath)
        .join(", ")}`,
  );
}

export async function resolveSingleNode(
  backend: IILSpyBackend,
  query: SymbolQueryInput,
  purpose: string,
  requiredCommand?: AvailableNodeCommands,
): Promise<Node> {
  let matches: Node[];

  if (isNonEmptyString(query.symbol)) {
    matches = pickPreferredSymbolMatches(
      await searchNodes(backend, query.symbol, query),
      query.symbol,
    );

    if (matches.length === 0) {
      throw new Error(
        `No symbol matched "${query.symbol}"${describeAssemblySuffix(query)}.`,
      );
    }

    if (matches.length > 1) {
      throw createAmbiguousNodeError(query.symbol, purpose, matches);
    }
  } else {
    matches = [await resolveAssemblyNode(backend, query)];
  }

  const match = matches[0];
  ensureNodeCan(match, purpose, requiredCommand);
  return match;
}

export function pickPreferredSymbolMatches(
  nodes: Node[],
  symbol: string,
): Node[] {
  if (nodes.length <= 1) {
    return nodes;
  }

  const exactMatches = nodes.filter((node) =>
    [node.metadata?.name, node.displayName, node.description]
      .filter(isNonEmptyString)
      .some((value) => isSameText(value, symbol)),
  );

  return exactMatches.length > 0 ? exactMatches : nodes;
}

export function summarizeAssemblyData(assemblyData: AssemblyData) {
  return {
    name: assemblyData.name,
    assemblyPath: assemblyData.filePath,
    version: assemblyData.version,
    targetFramework: assemblyData.targetFramework,
    autoLoaded: assemblyData.isAutoLoaded,
  };
}

export function summarizeAssemblyNode(node: Node) {
  const assemblyPath = node.metadata?.assemblyPath ?? "";
  const parsedPath = nodePath.parse(assemblyPath);

  return {
    name: node.metadata?.name ?? node.displayName,
    displayName: node.displayName,
    assemblyPath,
    fileName: parsedPath.base,
    description: node.description,
  };
}

export function summarizeNode(node: Node) {
  return {
    displayName: node.displayName,
    description: node.description,
    ...summarizeNodeMetadata(node.metadata, node.displayName),
  };
}

export function summarizeNodeMetadata(
  nodeMetadata: NodeMetadata | undefined,
  fallbackName = "",
) {
  return {
    name: nodeMetadata?.name ?? fallbackName,
    type: NodeType[nodeMetadata?.type ?? NodeType.Unknown],
    assemblyPath: nodeMetadata?.assemblyPath,
    symbolToken: nodeMetadata?.symbolToken,
    parentSymbolToken: nodeMetadata?.parentSymbolToken,
    subType: nodeMetadata?.subType,
    availableCommands: getAvailableCommandNames(
      nodeMetadata?.availableCommands ?? AvailableNodeCommands.None,
    ),
    nodeMetadata,
  };
}

export function hasAssemblyFilter(filter: AssemblyFilterInput) {
  return (
    isNonEmptyString(filter.assemblyPath) ||
    isNonEmptyString(filter.assemblyName)
  );
}

export function requireNodeMetadata(
  value: unknown,
  purpose: string,
): NodeMetadata {
  if (!isNodeMetadata(value)) {
    throw new Error(
      `A valid nodeMetadata object is required to ${purpose}. ` +
        `Use list_decompiler_nodes to get one from the ILSpy tree.`,
    );
  }

  return value;
}

function matchesAssembly(node: Node, filter?: AssemblyFilterInput) {
  if (!filter || !hasAssemblyFilter(filter)) {
    return true;
  }

  const assemblyPath = node.metadata?.assemblyPath;
  if (!assemblyPath) {
    return false;
  }

  const pathMatches =
    !isNonEmptyString(filter.assemblyPath) ||
    isSameText(assemblyPath, filter.assemblyPath);
  const nameMatches =
    !isNonEmptyString(filter.assemblyName) ||
    getAssemblyNames(node).some((value) =>
      isSameText(value, filter.assemblyName!),
    );

  return pathMatches && nameMatches;
}

function ensureNodeCan(
  node: Node,
  purpose: string,
  requiredCommand?: AvailableNodeCommands,
) {
  ensureNodeMetadataCan(node.metadata, node.displayName, purpose, requiredCommand);
}

export function ensureNodeMetadataCan(
  nodeMetadata: NodeMetadata | undefined,
  displayName: string,
  purpose: string,
  requiredCommand?: AvailableNodeCommands,
) {
  if (!requiredCommand || !nodeMetadata) {
    return;
  }

  const availableCommands =
    nodeMetadata.availableCommands ?? AvailableNodeCommands.None;
  if ((availableCommands & requiredCommand) !== requiredCommand) {
    throw new Error(`"${displayName}" cannot be used to ${purpose}.`);
  }
}

function createAmbiguousNodeError(
  symbol: string,
  purpose: string,
  matches: Node[],
) {
  return new Error(
    `Multiple symbols matched "${symbol}" while trying to ${purpose}. ` +
      `Narrow the request with assemblyPath or assemblyName. Candidates: ${matches
        .slice(0, 10)
        .map(
          (node) =>
            `${node.displayName} (${
              NodeType[node.metadata?.type ?? NodeType.Unknown]
            }) [${node.metadata?.assemblyPath ?? "unknown assembly"}]`,
        )
        .join("; ")}`,
  );
}

function describeAssemblyFilter(filter: AssemblyFilterInput) {
  if (
    isNonEmptyString(filter.assemblyPath) &&
    isNonEmptyString(filter.assemblyName)
  ) {
    return `assemblyPath "${filter.assemblyPath}" and assemblyName "${filter.assemblyName}"`;
  }

  if (isNonEmptyString(filter.assemblyPath)) {
    return `assemblyPath "${filter.assemblyPath}"`;
  }

  return `assemblyName "${filter.assemblyName}"`;
}

function describeAssemblySuffix(filter: AssemblyFilterInput) {
  return hasAssemblyFilter(filter)
    ? ` in ${describeAssemblyFilter(filter)}`
    : "";
}

function getAssemblyNames(node: Node) {
  const assemblyPath = node.metadata?.assemblyPath;
  if (!assemblyPath) {
    return [];
  }

  const parsedPath = nodePath.parse(assemblyPath);
  return [
    node.metadata?.name,
    node.displayName,
    parsedPath.base,
    parsedPath.name,
  ].filter(isNonEmptyString);
}

function getAvailableCommandNames(availableCommands: AvailableNodeCommands) {
  const commandNames: string[] = [];

  if ((availableCommands & AvailableNodeCommands.Decompile) !== 0) {
    commandNames.push("decompile");
  }
  if ((availableCommands & AvailableNodeCommands.Analyze) !== 0) {
    commandNames.push("analyze");
  }
  if ((availableCommands & AvailableNodeCommands.Export) !== 0) {
    commandNames.push("export");
  }
  if ((availableCommands & AvailableNodeCommands.ManageRootEntries) !== 0) {
    commandNames.push("manageRootEntries");
  }

  return commandNames;
}

function isSameText(left: string, right: string) {
  return left.trim().toLowerCase() === right.trim().toLowerCase();
}

function isNonEmptyString(value: string | undefined): value is string {
  return typeof value === "string" && value.trim().length > 0;
}

function isNodeMetadata(value: unknown): value is NodeMetadata {
  if (!value || typeof value !== "object") {
    return false;
  }

  const candidate = value as Record<string, unknown>;
  return (
    isNonEmptyString(asString(candidate.assemblyPath)) &&
    isNonEmptyString(asString(candidate.name)) &&
    isInteger(candidate.type) &&
    isInteger(candidate.symbolToken) &&
    isInteger(candidate.parentSymbolToken) &&
    isInteger(candidate.availableCommands) &&
    (!("bundledAssemblyName" in candidate) ||
      candidate.bundledAssemblyName === undefined ||
      isNonEmptyString(asString(candidate.bundledAssemblyName))) &&
    (!("subType" in candidate) ||
      candidate.subType === undefined ||
      isNonEmptyString(asString(candidate.subType)))
  );
}

function asString(value: unknown) {
  return typeof value === "string" ? value : undefined;
}

function isInteger(value: unknown): value is number {
  return typeof value === "number" && Number.isInteger(value);
}
