# AGENTS.md (vscode-extension)

Applies to files under `vscode-extension/`.
Also follow the repository root guide: [`../AGENTS.md`](../AGENTS.md).

Backend counterpart instructions: [`../backend/AGENTS.md`](../backend/AGENTS.md).

## Area overview

- Main implementation: `src/`
- Bundled output target: `dist/`
- Tests: unit tests run via Vitest (`test:unit`), extension tests via `test`
- Backend runtime payload expected at: `bin/ilspy-backend/` - placed there automatically when building backend

## Best-practice change rules

1. Keep command IDs, configuration keys, and context keys stable unless change is requested.
2. For protocol-facing changes, verify backend LSP handlers and DTOs stay compatible.
3. Prefer extending existing services/providers over adding new parallel abstractions.
4. Keep startup path robust: backend process launch and initialization should remain explicit and observable.

## Local commands

Run from `vscode-extension/` unless noted:

- Install deps: `pnpm install`
- Compile (dev): `pnpm compile`
- Lint: `pnpm lint`
- Unit tests: `pnpm test:unit`

When extension behavior depends on backend changes, refresh backend payload from repo root:

- `./buildtools/publish-backend`

## Coordination checklist (extension <-> backend)

- If adding/changing an LSP method or payload:
  - update backend implementation and tests;
  - update extension client calls and handling;
  - validate both side-specific tests relevant to the change.
