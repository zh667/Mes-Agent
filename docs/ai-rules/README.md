# AI Rule Docs

This directory contains detailed, on-demand rules for agents working on MES
Copilot. Keep `AGENTS.md` short and route task-specific guidance here.

## How To Use

1. Read `AGENTS.md`.
2. Open only the files relevant to the current task.
3. Prefer executable rules: concrete paths, commands, examples, and acceptance
   checks.
4. When project code contradicts these planning-stage defaults, follow the
   existing code and update the docs if the new pattern is intentional.

## File Map

- `engineering.md`: general development workflow and collaboration rules.
- `project-boundaries.md`: MES Copilot product scope, non-goals, domain model,
  Agent scenarios, and phased delivery.
- `typescript.md`: strict TypeScript and frontend safety rules.
- `api-contracts.md`: shared backend/frontend contract source of truth.
- `frontend.md`: React/Blazor frontend conventions and SignalR usage.
- `admin-ui.md`: manufacturing operations/admin UI standards.
- `backend.md`: ASP.NET Core, C#, Agent tools, REST, and SignalR rules.
- `database.md`: PostgreSQL/SQL Server, EF Core, migrations, and query rules.
- `errors.md`: validation, domain errors, HTTP responses, and Agent failures.
- `logging.md`: structured logs, audit trail, privacy, and observability.
- `testing.md`: unit, integration, E2E, and acceptance test expectations.

## Maintenance Rules

- Do not duplicate long examples in `AGENTS.md`.
- Add new details to the narrowest matching file.
- Preserve project-specific context when refreshing generated rules.
- Keep `CLAUDE.md` as a one-line include: `@AGENTS.md`.
- When commands or folder names become real in the codebase, update these docs
  to match the repository rather than the initial plan.
