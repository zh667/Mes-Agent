# Phase 3 Release Evidence

Date: 2026-07-10
Branch: `codex/phase3-tenancy`

## Build and Tests

| Gate | Command | Result |
| --- | --- | --- |
| Backend build | `dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false` | PASS, 0 warnings, 0 errors |
| Unit tests | `dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --no-build ...` | PASS, 264/264 in coverage run |
| Integration tests | `dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --no-build ...` | PASS, 86/86; includes PostgreSQL/Testcontainers verification, scheduling concurrency, and cross-tenant verification isolation |
| Format | `dotnet format MesCopilot.sln --verify-no-changes --no-restore` | PASS |
| Frontend lint | `pnpm --dir web lint` | PASS, no warnings |
| Frontend typecheck | `pnpm --dir web typecheck` | PASS |
| Frontend tests | `pnpm --dir web test` | PASS, 50/50 |
| Frontend build | `pnpm --dir web build` | PASS, 16 routes generated |
| Browser E2E | `pnpm --dir web exec playwright test` | PASS, 4/4 (auth boundary, i18n persistence, device credential UI, and BOM -> scheduling -> verified Agent workflow) |

## Coverage

Command: `dotnet test ... --settings tests\MesCopilot.UnitTests\coverage.runsettings --collect:"XPlat Code Coverage"`.

- Scoped core line coverage: **71.18%** (`3444/4838`), enforced threshold 70%.
- Agent: 87.35%.
- Application: 61.78%.
- Domain: 71.22%.
- Infrastructure and simulator results are reported separately and are not used to inflate the required core aggregate.

## Performance Baselines

BenchmarkDotNet `ShortRun`, .NET 8.0.25, Windows 11, Intel Core i5-12500H:

| Workload | Mean | Threshold |
| --- | ---: | ---: |
| 10,000-node BOM hot-path traversal | 246.8 us | < 500 ms |
| 50 work orders x 10 operation ordering workload | 12.77 us | < 2 s |
| Structured fact verification | 2.451 us | < 3 s excluding external query/LLM time |

These benchmarks isolate deterministic in-process hot paths. PostgreSQL query latency and end-to-end API latency remain covered by integration/load testing rather than being hidden inside microbenchmarks.

## Contracts and Deployment

- Swagger regenerated to `docs/openapi/mescopilot.v1.json`.
- TypeScript contract regenerated to `web/shared/api/generated/schema.ts`.
- Contract contains `VerificationResultDto` and `ApiProblemDetails.code`.
- Development and production modes of `scripts\verify-phase3-compose.ps1`: PASS; the production mode also rejected `.env.example` placeholders as expected.
- Base and replica Compose config: PASS.
- `docker compose build`: PASS for API and Web images.
- Redis and MQTT anonymous/authenticated smoke checks were completed during Phase 3A/3B review handling.
- Live Compose startup: PostgreSQL, Redis, MQTT, and API healthy; Web returned HTTP 200 and reached the API through `API_INTERNAL_URL`.
- Bootstrap administrator creation and direct API login were verified against the persistent Compose database.

## Security Scan Triage

The identifier scan for `password`, tokens, connection strings, API keys, and private keys returned only source field names, documented placeholders, generated migration metadata, and explicit test fixtures. No real secret value was found. Production still requires secret-manager supplied values described in the deployment runbook.

## Remaining Boundaries

Deferred production hardening remains tracked in `docs/superpowers/reviews/2026-07-10-phase3-review-followups.md`, including broker TLS profile, OPC UA revocation, least-privilege replica credentials, and dense Gantt paging.
