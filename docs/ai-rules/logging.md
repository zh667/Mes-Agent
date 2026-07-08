# Logging Rules

Use these rules for structured logs, audit logs, tracing, privacy, and
observability.

## Structured Logging

Log with named fields instead of concatenated strings.

Useful fields:

- `workOrderId`
- `workOrderNumber`
- `batchNumber`
- `operationId`
- `productionLineId`
- `equipmentId`
- `operatorId`
- `agentName`
- `toolName`
- `correlationId`

## Log Levels

- Debug: local diagnostic details that are safe and low volume.
- Information: important business events such as work order transitions.
- Warning: recoverable failures, retries, invalid state attempts.
- Error: failed operations requiring attention.
- Critical: system-wide outage or data integrity risk.

## Audit Logs

Audit high-impact manufacturing and Agent operations:

- Work order start, pause, report, complete, and close.
- Quality inspection result changes.
- Rework and scrap decisions.
- Agent tool calls.
- Knowledge base document indexing and deletion.
- Permission or role changes.

Audit records should include who, when, what action, target entity, important
parameters, and result.

## Privacy And Secrets

- Never log API keys, tokens, passwords, real connection strings, full prompts
  containing private data, or complete production/customer records.
- Mask sensitive fields.
- Treat uploaded documents as potentially confidential.
- Keep RAG source snippets limited to what the user is allowed to access.

## Tracing

- Propagate correlation IDs from HTTP requests into service logs, Agent tool
  calls, SignalR events, and background jobs.
- For LLM calls, log model/provider, latency, token counts, tool names, and
  success/failure. Do not log full sensitive prompts by default.
