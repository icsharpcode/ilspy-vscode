# AGENTS.md (backend/ILSpyX.Backend.LSP)

Applies to files under `backend/ILSpyX.Backend.LSP/`.
Also follow parent guides:
- [`../AGENTS.md`](../AGENTS.md)
- [`../../AGENTS.md`](../../AGENTS.md)

Backend core counterpart: [`../ILSpyX.Backend/AGENTS.md`](../ILSpyX.Backend/AGENTS.md).

## Local architecture map

- `Program.cs`: language server bootstrap, DI wiring, handler registration.
- `Protocol/Messages.cs`: JSON-RPC method names and request/response contracts.
- `Handlers/`: endpoint behavior that maps protocol messages to backend operations.

## Critical invariants

1. Keep method names in `[Method("ilspy/...")]` stable unless coordinated with extension client changes.
2. Keep request/response records in `Protocol/Messages.cs` backward-compatible when possible.
3. New handlers must be registered in `Program.cs` and use DI-managed backend services.
4. Preserve `ShouldUpdateAssemblyList` semantics for responses that detect auto-loaded assembly changes.

## Change checklist

- If adding/changing an LSP method:
  - update protocol records in `Protocol/Messages.cs`;
  - implement/update handler in `Handlers/`;
  - register handler in `Program.cs`;
  - align extension client usage and tests.

- Prefer keeping handlers thin:
  - delegate feature logic to `ILSpyX.Backend` services;
  - keep transport mapping and response shaping in LSP layer.

## Validation commands

From `backend/`:

- `dotnet build ILSpy-backend.sln`
- `dotnet test ILSpyX.Backend.LSP.Tests/ILSpyX.Backend.LSP.Tests.csproj`
