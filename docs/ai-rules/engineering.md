# Engineering Rules

## Start Of Task

1. Restate the goal and the acceptance criteria when the task is ambiguous.
2. Inspect repository state with `git status` when this is a git repository.
3. Read relevant source, tests, and docs before changing files.
4. For non-trivial work, make a short plan covering files, validation, and risks.
5. If the task touches API contracts, data models, Agent tools, or MES workflows,
   list edge cases before implementation.

## Implementation Discipline

- Keep the diff small and intentional.
- Follow existing project style before introducing a new pattern.
- Prefer typed, structured APIs over ad hoc string parsing.
- Do not introduce new NuGet or npm packages without a clear purpose and a
  lower-complexity alternative check.
- Do not hide failures. Surface build, test, migration, or runtime errors with
  the command that produced them.
- Use comments sparingly. Explain non-obvious business rules and tradeoffs, not
  code that is already readable.

## Source Control

- Do not develop directly on `main` for implementation work.
- Use Conventional Commits when committing:
  - `feat(agent): add production progress tool`
  - `fix(api): handle missing product in work order query`
  - `test(quality): add quality trace integration tests`
  - `docs(rules): refresh AI collaboration guide`
- Never force-push shared branches without explicit user approval.
- Never revert user changes unless the user specifically asks.

## Finish Of Task

1. Run validation commands appropriate to the change.
2. Read the diff before reporting completion.
3. Confirm there are no debug statements, temporary files, generated junk,
   accidental formatting churn, or secret-like values.
4. Summarize in Chinese: changed files, verification, and residual risk.
