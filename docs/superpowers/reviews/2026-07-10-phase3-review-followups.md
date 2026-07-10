# Phase 3 Review Follow-ups

Updated: 2026-07-11

This file records review findings that are valid but intentionally deferred beyond the current Phase 3A/3B merge scope. Items include the existing mitigation and the condition that should trigger implementation.

## Resolved Follow-ups

| ID | Resolution | Verification |
| --- | --- | --- |
| P3-F11 | All checked-in operations UI copy was moved into matching `en-US` and `zh-CN` feature catalogs. A TypeScript-AST checker now rejects hardcoded JSX text and accessible labels; locale parity is enforced by tests and CI. | `pnpm check:i18n`, frontend unit tests, lint, typecheck, build, and localization Playwright coverage. |
| P3-F15 | Modbus transport I/O now uses a five-second deadline independent of the polling cadence. The simulator test has separate startup, connection, and first-reading budgets, and a delayed-loopback regression proves a 300 ms response succeeds with 100 ms polling. | Targeted delayed-response regression, repeated Modbus integration tests, and the complete integration suite. |

## Deferred Hardening

| ID | Finding | Current mitigation | Follow-up acceptance |
| --- | --- | --- | --- |
| P3-F01 | OPC UA certificate revocation is not checked through CRL or OCSP. | Production requires secure mode and an explicitly pinned thumbprint; untrusted certificates are not auto-accepted. | Define the plant PKI lifecycle, configure issuer/revocation stores, and reject a revoked integration-test certificate. |
| P3-F02 | Modbus TCP does not enable socket keepalive. | Connect/read operations have bounded timeouts and the collector reconnects with capped backoff. | Add configurable keepalive probes and fault-injection coverage for a half-open PLC connection. |
| P3-F03 | The development Compose profile exposes PostgreSQL on the host and has local fallback credentials. | `.env.example` marks local-only replacement values; production secrets are not committed. | Add a production Compose/hosting profile with no database host port and secret-store supplied credentials. |
| P3-F04 | BOM explosion loads all active BOMs for the current tenant before traversing the reachable graph. | Tenant filtering, a maximum depth of 20, and request quantity limits bound the current demo workload. | Replace the tenant-wide preload with a reachable recursive query or closure table and benchmark a tenant with at least 100,000 BOM items. |
| P3-F05 | Replica reads currently use the PostgreSQL application/superuser credentials in the optional Compose overlay. | The overlay is local-only and not enabled by default. | Provision a least-privilege read-only role, remove superuser credentials from replica reads, and verify writes fail through the replica connection. |
| P3-F06 | Production startup does not yet fail fast for every placeholder JWT, database, and audit HMAC value. | Redis and Data Protection key persistence now fail fast in Production; no real secret is committed. | Add typed production configuration validation for JWT secret, primary/read database credentials, audit HMAC key, and device policy. |
| P3-F07 | Local Mosquitto uses authenticated but non-TLS transport. | The broker is internal-only, anonymous access and topic spoofing are blocked, and device MQTT credentials require TLS through the production connector policy. | Add a certificate-backed production broker profile and an integration test that rejects an untrusted broker certificate. |
| P3-F08 | Device protocol health is not part of the API `/health` readiness response. | `/health` checks PostgreSQL and Redis; per-device connection errors and last-connect timestamps are exposed in the device registry. | Add a separate device-ingestion health/metrics endpoint with partial-degradation semantics; do not make one offline PLC restart the API. |
| P3-F09 | Gantt has a 31-day range limit but no explicit row/page cap inside that range. | The date-range guard prevents unbounded historical scans. | Add server-side row limits or cursor paging and load-test dense schedules before production rollout. |
| P3-F10 | MQTT persistence remains disabled for current-state telemetry. | Equipment state is persisted by the API; the broker is not the system of record. | Revisit persistence and QoS storage if alarms or commands become durable MQTT business events. |
| P3-F12 | Persisted message verification has GET/recheck APIs, but the current chat scaffold does not restore a selected historical conversation after page reload. | Verification JSON is versioned on the assistant message; corrupt data fails closed as Unverified. | When conversation history selection is added to the Agent UI, fetch each assistant message verification and prove reload parity with an E2E test. |
| P3-F13 | BOM and scheduling BenchmarkDotNet baselines isolate deterministic 10,000-node/500-operation hot paths rather than invoking EF Core and PostgreSQL end to end. | PostgreSQL correctness and concurrency are covered separately by Testcontainers integration tests. | Add database-backed load scenarios and enforce p95 thresholds in CI when production-like data volume and runner hardware are available. |
| P3-F14 | The cross-module Playwright workflow exercises the real UI and generated client contracts with mocked HTTP/SSE transport rather than a live API and PostgreSQL. | Controller, tenant isolation, migration, and scheduling concurrency behavior are covered by separate integration tests. | Run the same BOM -> scheduling -> verified Agent workflow against a disposable full Compose/Testcontainers environment in CI. |

## Findings Not Applied

| Review ID | Decision and evidence |
| --- | --- |
| B3 / B6 | The global filter is already fail-closed: `TenantId == null` matches no non-null tenant row. `Queries_WithoutTenantContext_ReturnNoBusinessData` proves this behavior. Global integer primary keys also prevent the claimed cross-tenant duplicate material key. |
| B4 | EF Core executes a migration in a transaction by default. No operation in `AddTenantIsolation` suppresses the transaction; a failed constraint rolls the migration back. |
| B5 | `Tenant` is the partition root and `UserTenantMembership` is the control-plane relation used to resolve a tenant. Making either an `ITenantEntity` would prevent membership lookup before the tenant context exists. Tenant administration endpoints retain explicit authorization and user scoping. |
| H4 / M16 | The first migration backfills memberships from `AspNetUsers.Role`; only the next ordered migration removes that column. EF applies migrations sequentially and transactionally. The non-empty phase-two migration test covers this upgrade path. |
| H11 | Revocation is checked in `OnTokenValidated`, after signature/lifetime validation and before authorization. Moving it earlier would inspect an untrusted token and cannot eliminate distributed-store latency. |
| M11 | Tenant membership authorization uses the primary scoped context, not the replica read factory, so the five-second report/audit read sticky window does not govern permission changes. |
| M12 | Cross-tenant platform audit querying is not part of the current tenant-scoped audit contract. Platform administrators must explicitly select an authorized tenant. |
| M13 | Coordinator scopes are initialized with a specific tenant and all runtime queries retain global filters; `IsPlatformAdmin` does not disable those filters. |
| L2 | The API Dockerfile explicitly installs `curl`; the Compose health check is executable in the final image. |
| L4 / L5 | Tenant switching is a validated active-tenant header selection, not an identity change. Role replacement intentionally runs after authentication and before authorization. |
