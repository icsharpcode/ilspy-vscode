import { beforeEach, describe, expect, it, vi } from "vitest";
import type OutputWindowLogger from "../OutputWindowLogger";

const {
  executeCommand,
  showWarningMessage,
  getCachedDotnetRuntimePath,
  cacheDotnetRuntimePath,
  existsSync,
} = vi.hoisted(() => ({
  executeCommand: vi.fn(),
  showWarningMessage: vi.fn(),
  getCachedDotnetRuntimePath: vi.fn(),
  cacheDotnetRuntimePath: vi.fn(),
  existsSync: vi.fn(),
}));

vi.mock("vscode", () => ({
  commands: {
    executeCommand,
  },
  window: {
    showWarningMessage,
  },
}));

vi.mock("../decompiler/settings", () => ({
  getCachedDotnetRuntimePath,
  cacheDotnetRuntimePath,
}));

vi.mock("fs", () => ({
  existsSync,
}));

import { resolveDotnetRuntime } from "./resolveDotnetRuntime";

function createContext() {
  return {
    extension: {
      id: "icsharpcode.ilspy-vscode",
    },
  };
}

function createLogger() {
  return {
    writeLine: vi.fn(),
  } as unknown as OutputWindowLogger;
}

describe("resolveDotnetRuntime", () => {
  beforeEach(() => {
    vi.clearAllMocks();
  });

  it("defers runtime refresh until the caller starts it", async () => {
    const context = createContext();
    const logger = createLogger();
    getCachedDotnetRuntimePath.mockReturnValue("/cached/dotnet");
    existsSync.mockReturnValue(true);
    executeCommand.mockImplementation(async (command: string) => {
      if (command === "dotnet.acquire") {
        return { dotnetPath: "/updated/dotnet" };
      }
    });

    const result = await resolveDotnetRuntime(context as never, logger);

    expect(result.dotnetPath).toBe("/cached/dotnet");
    expect(result.refreshRuntimeInBackground).toBeTypeOf("function");
    expect(executeCommand).not.toHaveBeenCalled();
    expect(cacheDotnetRuntimePath).not.toHaveBeenCalled();

    await result.refreshRuntimeInBackground?.();

    expect(executeCommand).toHaveBeenNthCalledWith(1, "dotnet.acquire", {
      version: "10.0",
      requestingExtensionId: "icsharpcode.ilspy-vscode",
    });
    expect(executeCommand).toHaveBeenNthCalledWith(
      2,
      "dotnet.ensureDotnetDependencies",
      {
        command: "/updated/dotnet",
        arguments: ["--info"],
      }
    );
    expect(cacheDotnetRuntimePath).toHaveBeenCalledWith(
      context,
      "10.0",
      "/updated/dotnet"
    );
  });

  it("reacquires the runtime immediately when the cached path is gone", async () => {
    const context = createContext();
    const logger = createLogger();
    getCachedDotnetRuntimePath.mockReturnValue("/cached/dotnet");
    existsSync.mockReturnValue(false);
    executeCommand.mockImplementation(async (command: string) => {
      if (command === "dotnet.acquire") {
        return { dotnetPath: "/fresh/dotnet" };
      }
    });

    const result = await resolveDotnetRuntime(context as never, logger);

    expect(result.dotnetPath).toBe("/fresh/dotnet");
    expect(result.refreshRuntimeInBackground).toBeUndefined();
    expect(cacheDotnetRuntimePath).toHaveBeenCalledWith(
      context,
      "10.0",
      "/fresh/dotnet"
    );
    expect(showWarningMessage).not.toHaveBeenCalled();
  });
});
