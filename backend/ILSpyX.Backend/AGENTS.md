# AGENTS.md (backend/ILSpyX.Backend)

Applies to files under `backend/ILSpyX.Backend/`.
Also follow parent guides:
- [`../AGENTS.md`](../AGENTS.md)
- [`../../AGENTS.md`](../../AGENTS.md)

LSP integration counterpart: [`../ILSpyX.Backend.LSP/AGENTS.md`](../ILSpyX.Backend.LSP/AGENTS.md).

## Local architecture map

- `Decompiler/`: assembly loading, decompilation, metadata access, and code rendering.
- `TreeProviders/`: node graph expansion and node-type dispatch (`TreeNodeProviders`).
- `Search/` and `Analyzers/`: feature backends returning tree-compatible results.
- `Model/`: shared node/metadata/contracts used by both backend and LSP host.

## Critical invariants

1. Keep `Model/*` stable and serializable for LSP transport.
2. Register new services/providers in `ILSpyXBackendServices` extension methods.
3. When introducing a new `NodeType`/symbol mapping, update `TreeNodeProviders` dispatch.
4. Preserve deterministic output ordering where results are user-visible.

## Change checklist

- If adding a new tree node category:
  - add/adjust model shape in `Model/` as needed;
  - implement provider behavior in `TreeProviders/`;
  - wire provider registration and dispatch;
  - update tests in `../ILSpyX.Backend.Tests/`.

- If changing decompilation behavior:
  - validate `DecompilerBackend` paths for assembly/type/member handles;
  - check effects on search/analyze features that rely on shared metadata.

## Validation commands

From `backend/`:

- `dotnet build ILSpy-backend.sln`
- `dotnet test ILSpyX.Backend.Tests/ILSpyX.Backend.Tests.csproj`
