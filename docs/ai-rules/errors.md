# Error Rules

Use these rules for validation errors, domain exceptions, HTTP responses, Agent
tool failures, and user-facing error messages.

## Error Categories

- Validation error: request shape or field value is invalid.
- Not found: entity does not exist or is not visible to the user.
- Conflict: state transition or concurrency conflict.
- Authorization error: user cannot perform the action.
- External dependency error: database, vector store, LLM, file parser, or network
  dependency failed.
- Domain error: MES business rule prevents the operation.

## HTTP Responses

- Use consistent problem details for API errors.
- Validation errors should identify field names.
- Domain errors should include a stable error code.
- Do not leak stack traces, connection strings, prompts, tokens, or private data
  in API responses.

Examples of useful domain codes:

- `WORK_ORDER_NOT_FOUND`
- `WORK_ORDER_INVALID_STATE`
- `QUALITY_TRACE_NOT_FOUND`
- `EQUIPMENT_STATUS_CONFLICT`
- `AGENT_TOOL_VALIDATION_FAILED`

## Domain Exceptions

- Prefer specific exceptions for domain failures:
  - `WorkOrderNotFoundException`
  - `InvalidWorkOrderStateException`
  - `QualityTraceNotFoundException`
- Convert exceptions to HTTP responses in middleware or filters.
- Include context in logs, not in unsafe public responses.

## Agent Failures

- Agent tools must validate inputs before calling services.
- Tool failures should be visible to the caller with a concise, actionable
  message.
- The Agent should not invent data when a tool fails.
- If retrieval returns low confidence or no source documents, say that directly
  and ask for more context or data.

## User-Facing Copy

- Use clear operational language.
- Say what failed, what record was affected, and what can be done next.
- Avoid vague messages such as `Something went wrong`.
