# AGENTS.md (backend)

Applies to files under `backend/`.
Also follow the repository root guide: [`../AGENTS.md`](../AGENTS.md).

Extension counterpart instructions: [`../vscode-extension/AGENTS.md`](../vscode-extension/AGENTS.md).

## Area overview

- Solution: `ILSpy-backend.sln`
- Core backend library: `ILSpyX.Backend/`
- LSP host/service: `ILSpyX.Backend.LSP/`
- Tests: `ILSpyX.Backend.Tests/` (for core backend), `ILSpyX.Backend.LSP.Tests/` (for LSP layer)

## Best-practice change rules

1. Keep LSP method names and payload contracts explicit and version-safe.
2. Prefer strongly typed request/response models over ad-hoc JSON handling.
3. Keep backend as the primary home for domain/business behavior and decision-making (for example, which actions are available, which node capabilities/attributes are exposed, and which data is returned).
4. Keep decompilation/search/analyze behavior deterministic for identical inputs.
5. When changing public behavior, update or add tests in the closest test project.

## Local commands

Run from `backend/` unless noted:

- Build: `dotnet build ILSpy-backend.sln`
- Test all backend tests: `dotnet test ILSpy-backend.sln`
- Test backend core only: `dotnet test ILSpyX.Backend.Tests/ILSpyX.Backend.Tests.csproj`
- Test LSP layer only: `dotnet test ILSpyX.Backend.LSP.Tests/ILSpyX.Backend.LSP.Tests.csproj`

From repo root, publish backend payload for extension integration testing:

- `./buildtools/publish-backend`

## Coordination checklist (backend <-> extension)

- For LSP contract changes:
  - update extension client usage and error handling;
  - verify extension-side behavior where the method is consumed;
  - run relevant tests in both `backend/` and `vscode-extension/`.
