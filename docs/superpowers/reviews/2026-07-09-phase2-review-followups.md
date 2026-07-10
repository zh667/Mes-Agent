# Phase 2 Review Follow-ups

Date: 2026-07-09

This file tracks review items that were intentionally not fixed in the current
review-handling pass. It exists so "record-only" feedback does not live only in
chat history.

## Auth Hardening

- Refresh token rotation still has a concurrent refresh race. Current status:
  recorded inline in `src/MesCopilot.Api/Controllers/AuthController.cs` as a
  Phase 2 hardening TODO. Preferred fix: compare-and-swap token rotation or EF
  optimistic concurrency.
- OpenAPI documentation for `/api/Auth/logout` should be regenerated/aligned
  with its actual `204 No Content` response.
- OpenAPI documentation for `/api/Auth/me` and `/api/Auth/logout` should include
  `401 Unauthorized`.
- Frontend access token lifetime is currently coupled to backend defaults.
  Consider documenting or exposing a `NEXT_PUBLIC_ACCESS_TOKEN_LIFETIME_MS`
  value if backend lifetime becomes configurable per environment.

## Agent And RAG Follow-ups

- `FactVerifier` may over-detect bare numbers such as version strings. Tighten
  claim extraction before using it on mixed technical prose.
- `VerifiedRagAnswerGenerator` naming suggests full fact verification, while
  the current behavior mainly formats cited RAG answers. Rename or expand tests
  when the verification boundary is finalized.

## Phase 2C / 2D Follow-ups

- `AgentController` repeats some tool-selection predicates. Clean up only when
  adding more routing cases or when the duplication causes a behavior change.
- `AgentController` parses SOP codes with a per-call regex. Consider static or
  generated regex if this endpoint becomes hot.
- Conversation message tool results are deserialized as `object`/`JsonElement`.
  Keep this until strong typed tool result contracts are needed by the frontend.
- `SuggestScheduleTool` loads all work orders before filtering. Add filtered
  service queries before using this on large production datasets.
- `MaintenancePredictionService` uses a fallback MTBF value for zero-downtime
  equipment. Promote this to a named constant with rationale before production
  tuning.
- The chat UI currently identifies streaming assistant messages by display
  timestamp. Replace with an explicit UI state flag before adding localization.
- The structured result side panel is optimized for work order-shaped tool
  results. Add adapters for quality, OEE, knowledge, and maintenance tool data.

## Phase 2E / 2F / 2G Follow-ups

- `ConversationService.SearchConversationsAsync` currently uses
  `LOWER(Content) LIKE '%query%'` semantics and then groups in memory. It has a
  capped `take`, but should move to PostgreSQL full-text query and database-side
  paging before production-sized conversation history.
- Production and quality report export still depend on broad service reads before
  in-memory filtering. Add filtered report-specific queries before large data
  sets are expected.
- PDF report generation truncates tables to the first 50 rows without adding a
  visible "showing 50 of N" note. Add truncation disclosure when polishing
  report UX.
- Report endpoints use non-nullable `DateTime` query parameters with
  `default(DateTime)` fallback. Switch to `DateTime?` to distinguish omitted
  dates from invalid or explicit `0001-01-01` values.
- Long-running report generators do not accept `CancellationToken`, so client
  disconnects cannot stop expensive Excel/PDF generation yet.
- `DocumentsController.UploadNewVersion` relies on authenticated requests to
  provide a user id claim, while service validation handles missing ids. Keep an
  explicit controller-level check if alternate auth schemes are added.
- `KnowledgeService` still has a metadata-only constructor used by unit tests.
  Keep DI coverage around the full constructor so parser/vector dependencies are
  present in application runtime.

## Test Coverage Gaps

- Add deeper `ConversationService` tests for create, add message, context
  window, deletion, and ownership edge cases.
- Add tool-level tests for `PredictMaintenanceTool`, `SuggestScheduleTool`, and
  `FiveWhyAnalysisTool`.
- Add service tests for maintenance prediction and quality root-cause analysis
  boundary cases.
- Add frontend streaming hook tests for abort behavior and server-side error
  events.
- Add SSE integration coverage for cancellation and error event paths.

## Resolved By Phase 3

- `MesPromptBuilder` now validates the mode separately and raises
  `FileNotFoundException` with locale/mode context when a known localized
  template is missing. Covered by `LocalizedPromptBuilderTests`; verified on
  2026-07-10 with the full `255`-test unit run before the final coverage pass.
