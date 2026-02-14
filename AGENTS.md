# AGENTS.md (repository root)

This file provides baseline agent instructions for the whole repository.
Subproject files in `vscode-extension/AGENTS.md` and `backend/AGENTS.md` add local rules.

## Scope and precedence

1. Follow this file for any change in this repository.
2. When working inside a subproject, also follow that subproject's `AGENTS.md`.
3. If instructions conflict, the more specific (deeper) `AGENTS.md` wins for files in its scope.

## Project map

- `vscode-extension/`: TypeScript VS Code extension frontend and process orchestration.
- `backend/`: C#/.NET backend and LSP service.
- `buildtools/`: Helper scripts for packaging/testing across both parts.

Cross-project references:
- Extension guide: [`vscode-extension/AGENTS.md`](./vscode-extension/AGENTS.md)
- Backend guide: [`backend/AGENTS.md`](./backend/AGENTS.md)

## Best-practice workflow

1. Read related `README.md` and local `AGENTS.md` before editing.
2. Make the smallest possible change that solves the requested task.
3. Keep extension/backend contracts in sync (especially LSP message payloads and names).
4. Run focused validation first, then broader checks only if needed.
5. Summarize what changed, what was validated, and any follow-up risk.

## Validation commands (common entry points)

- Build/package backend for extension usage: `./buildtools/publish-backend`
- Build VSIX package: `./buildtools/build-vsix`
- Backend tests: `./buildtools/test-backend`
- Extension unit tests: `./buildtools/test-extension`

## Guardrails

- Do not introduce new tooling when existing scripts already cover the task.
- Avoid wide refactors unless explicitly requested.
- Preserve backward compatibility for user-visible commands/settings unless asked to change behavior.
