# AGENTS.md

This is the root instruction file for AI coding agents working on this
repository. Read this file first, then open only the detailed rule docs that
match the current task.

Claude Code reads `CLAUDE.md`, which should contain only:

```md
@AGENTS.md
```

## Project Snapshot

- Project name: MES Copilot
- Goal: build an intelligent manufacturing operations Agent platform for MES
  data queries, anomaly analysis, quality traceability, SOP/RAG answers, and
  production reports.
- Planned backend stack: ASP.NET Core Web API, EF Core, PostgreSQL or SQL
  Server, SignalR, Worker Service, BotSharp or Microsoft Agent Framework.
- Planned frontend stack: React/TypeScript or Blazor, with an operations/admin
  UI for shop-floor workflows.
- Current source tree may still be in planning stage. Follow the intended
  structure below unless the implemented codebase establishes a better pattern.

```text
mes-agent/
  src/
    MesCopilot.Api/
    MesCopilot.Core/
    MesCopilot.Infrastructure/
    MesCopilot.Agent/
    MesCopilot.Contracts/
    MesCopilot.Worker/
    MesCopilot.Web/
  tests/
    MesCopilot.UnitTests/
    MesCopilot.IntegrationTests/
  docs/
    ai-rules/
    spec.md
    plan.md
```

## Required Rules

1. Frontend and backend must share API contracts. Request/response schemas and
   inferred types must live in a shared contract layer. Frontend code must not
   hand-write backend response types.
2. Do not commit secrets, real connection strings, API keys, production data, or
   private customer/manufacturing data.
3. Keep changes scoped to the task. Do not mix feature work, refactors, and
   formatting churn unless the task explicitly requires it.
4. Preserve user changes. If existing uncommitted work is present, work around it
   and never revert it without explicit approval.
5. Do not claim checks passed unless the exact commands were run. If a command
   cannot run, report why.
6. Prefer existing project patterns, helpers, folder structure, and naming before
   adding new abstractions.
7. For non-trivial API, data model, Agent tool, or UI changes, identify edge
   cases before implementation.
8. All user-visible frontend copy, including accessible names, empty states,
   validation feedback, and status labels, must come from the `en-US` and
   `zh-CN` message catalogs. Add both translations in the same change; do not
   use hardcoded UI text or a one-locale fallback.

## Command Guide

Use detected project files first. If the project has not been initialized yet,
these are the expected defaults:

```powershell
dotnet restore
dotnet build
dotnet test
dotnet format --verify-no-changes
dotnet format
```

For the frontend, use the detected package manager from lockfiles or
`packageManager` in `package.json`. Prefer `pnpm` for new frontend work.

```powershell
pnpm install
pnpm lint
pnpm typecheck
pnpm test
pnpm build
```

## Detailed Rule Map

| Task type                                                      | Read these files                                                                  |
| -------------------------------------------------------------- | --------------------------------------------------------------------------------- |
| Any implementation task                                        | `docs/ai-rules/engineering.md`, `docs/ai-rules/project-boundaries.md`             |
| C# backend, Web API, SignalR, Agent tools                      | `docs/ai-rules/csharp-dotnet.md`, `docs/ai-rules/backend.md`, `docs/ai-rules/errors.md` |
| API DTOs, generated clients, frontend/backend contract changes | `docs/ai-rules/api-contracts.md`                                                  |
| Database, EF Core, migrations, queries                         | `docs/ai-rules/database.md`, `docs/ai-rules/csharp-dotnet.md`                     |
| React, TypeScript, generated API client usage                  | `docs/ai-rules/typescript.md`, `docs/ai-rules/frontend.md`                        |
| Blazor or shop-floor/admin screens                             | `docs/ai-rules/frontend.md`, `docs/ai-rules/admin-ui.md`                          |
| Tests, fixtures, acceptance checks                             | `docs/ai-rules/testing.md`                                                        |
| Agent plugins, tools, BotSharp integration                     | `docs/ai-rules/csharp-dotnet.md`, `docs/ai-rules/agent-development.md`            |
| AI rule maintenance                                            | `docs/ai-rules/README.md`, then the specific file being edited                    |

## Delivery Checklist

Before finishing a code task:

1. Run the smallest meaningful validation set for the change.
2. Read the diff and remove debug code, temporary files, accidental formatting
   churn, and secret-like values.
3. Summarize in Chinese: what changed, how it was verified, and remaining risk.
4. If a commit is requested, propose a Conventional Commit message first.
5. For frontend changes, run `pnpm check:i18n` and confirm both locale catalogs
   have matching, non-empty keys and no unapproved hardcoded visible JSX text.
