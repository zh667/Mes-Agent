# Phase 3 Complete Review Resolution

Updated: 2026-07-10

This document records the evidence-based disposition of `2026-07-10-phase3-complete-review.md`. Findings were checked against the current `codex/phase3-tenancy` worktree; stale findings were not implemented.

## Blocking Findings

| IDs | Decision | Evidence or action |
| --- | --- | --- |
| B1 | Not applicable | `EquipmentHub` already requires `RequireOperator`; endpoint and hub security tests cover anonymous rejection, membership, equipment ownership, and tenant-scoped groups. |
| B2, B14 | Not applicable | Tenant filters fail closed when no tenant is initialized. `Queries_WithoutTenantContext_ReturnNoBusinessData` proves no business rows are returned; material IDs are globally unique. |
| B3 | Not applicable by design | `Tenant` is the partition root and membership is the control-plane relation needed before tenant context exists. Applying the business-row filter to either would prevent tenant resolution. |
| B4 | Not applicable | Recheck resolves the message through tenant-filtered `ConversationService`. A new cross-tenant integration test proves another tenant's message returns 404. |
| B5 | Not applicable | Platform-admin claims and policy checks use case-insensitive comparison consistently. |
| B6 | Not applicable | Schedule adjustment uses a serializable transaction, a PostgreSQL tenant/equipment advisory lock, in-transaction overlap validation, and a real PostgreSQL concurrency test. |
| B7 | Not applicable | EF Core runs each migration transactionally unless transaction suppression is requested; this migration does not suppress it. |
| B8 | Misdiagnosed | The current Agent controller is a deterministic tool router and does not build an LLM prompt. Localized prompt templates and `MesPromptBuilder` are covered independently. Remaining operations copy is tracked as P3-F11. |
| B9, B10 | Fixed | Agent validation/SSE errors and Auth failures now return localized, stable problem codes. Chinese and English integration tests cover message validation, search validation, and invalid login. |
| B11 | Not user-facing | A missing resource key is a fail-fast programming error. Production exception handling does not expose the internal exception text; changing it would hide an invalid deployment. |
| B12 | Fixed | `verify-phase3-compose.ps1 -Production -EnvironmentFile .env` validates the real environment and rejects missing, short, default, and placeholder secrets without printing their values. |
| B13 | Fixed | The runbook now uses explicit host, port, user, database, password context, validates the dump, and documents restore to a new database. |

## High Findings

| IDs | Decision | Evidence or action |
| --- | --- | --- |
| H1, H2 | Not applicable | Compose enables Redis `requirepass` and Mosquitto password/ACL configuration with anonymous access disabled. |
| H3 | Partially deferred | Production connector policy rejects credentials without TLS. A certificate-backed broker profile remains P3-F07. |
| H4 | Not applicable | OPC UA secure mode requires a pinned thumbprint unless explicit insecure development mode is enabled; production disables insecure mode. |
| H5 | Not applicable | Production persists Data Protection keys to a required volume and fails fast when the path is absent. Provider-recreation tests cover key reuse. |
| H6 | Mis-scoped | `/health` checks PostgreSQL and Redis. Per-device degradation must not make the entire API unready and is tracked as P3-F08. |
| H7 | Mitigated | The ordered prior migration copies the legacy role into the default membership, and `Down` restores it. Production rollback uses backup restore rather than destructive down migrations. |
| H8-H12 | Not applicable | Existing fail-closed tenant tests, coordinator error handling, audit context, equipment ownership checks, work-order-state checks, and scheduling concurrency tests cover these claims. |
| H13 | Fixed | Knowledge citation verification rejects more than 100 distinct document IDs before issuing a database query; a unit test asserts zero query calls. |
| H14 | Not applicable | Revocation is checked after cryptographic token validation and before authorization, which is the correct trust boundary. |
| H15, H16 | Not applicable | Generated OpenAPI marks BOM request fields required and documents 404/409 responses. |
| H17 | Not a Compose-script responsibility | Tenant/auth/migration correctness is covered by integration tests. The script now validates production secrets, topology, persistent volumes, and optional runtime health. |
| H18 | Not applicable | The rollback runbook preserves the failed upgrade and audit evidence and restores into a new database. |
| H19 | Not applicable | Inventory availability is allocated once across repeated material paths; regression coverage exists. |
| H20 | Fixed | CI now collects scoped core coverage and fails below 70%. |

## Medium Findings

- Fixed now: M9 adds a database check constraint for verification JSON/schema-version consistency; M17 logs verification-query failures with tool and correlation ID; M20 checks the replica volume; M22/M23 add release evidence and secret/key-rotation guidance; M28 adds a BOM-to-scheduling-to-verified-Agent Playwright workflow; M29 runs short BenchmarkDotNet baselines in CI.
- Already fixed or disproved: M2, M4, M6, M8, M12-M16, M18, M21, M30.
- Intentionally deferred with acceptance criteria: M1/P3-F01, M3/P3-F02, M7/P3-F03, M10/P3-F09, M11/P3-F04, M19/P3-F11, M24, M25/P3-F13, and device/broker hardening P3-F07/P3-F08/P3-F10.
- Test-only observations M26 and M27 do not describe production behavior. The browser workflow uses mocked transport while controller/database behavior is tested separately; a live full-stack browser workflow is tracked below.

## Additional Verified Fixes

The new cross-module E2E test exposed a real issue not listed in the review: the Agent SSE hook sent the access token but omitted `X-Tenant-Id`. The hook now sends the validated active tenant, with unit and Playwright coverage.

Live Compose acceptance exposed two more configuration gaps. Compose now passes the existing bootstrap-administrator settings to the API, and NextAuth uses `API_INTERNAL_URL` for server-side login/refresh while preserving `NEXT_PUBLIC_API_URL` for browser traffic. The local administrator lives only in the ignored `.env`; no credential was added to tracked files.
