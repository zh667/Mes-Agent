# MES Copilot User Manual

MES Copilot is a manufacturing operations assistant for work orders, equipment, quality traceability, knowledge-base answers, and OEE analysis.

## Opening the Console

With Docker Compose running, open:

```text
http://localhost:3000
```

The home page links to the primary Phase 1 modules:

- Agent chat
- Work orders
- Equipment monitor
- Quality trace
- Knowledge base

## Agent Chat

Use Agent chat for natural-language MES questions. The Phase 1 interface supports mode selection so operators can keep questions scoped to the right operating domain.

Example questions:

```text
Show delayed work orders for today.
Trace batch B20260709-A102.
Calculate OEE for equipment 102 today.
Find SOP guidance for A102 alarm recovery.
Which process step has the most surface scratches?
```

## Agent Modes

| Mode | Use For | Typical Output |
| --- | --- | --- |
| Production | Delayed orders, dispatch state, production progress | Work order lists, totals, risk explanations |
| Quality | Batch traceability and defect analysis | Timeline, inspections, defect counts |
| Equipment | Equipment status and alarms | Current state, alarm context, status history |
| Knowledge | SOP and manual search | RAG answer, source documents, retrieved chunks |
| OEE | Availability, performance, quality, OEE | OEE components and production totals |

## Structured Data Output

Agent responses include a human-readable explanation and structured data. The structured panel is intended for:

- confirming exact work order IDs and batch numbers
- reviewing counts and metrics without parsing prose
- handing results to future workflow actions
- debugging generated answers against source data

For example, a delayed work order answer returns `workOrders` and `totalCount`; an OEE answer returns `availability`, `performance`, `quality`, and `oee`.

## Debug Mode

Enable debug mode when validating a response or investigating data lineage. Debug output can include:

- tool name called by the Agent
- execution time
- data source, such as database, vector search, or response cache

Debug mode is for operators, analysts, and developers who need traceability. It should not be treated as the final user-facing explanation.

## Work Orders

The Work Orders page shows a dense dispatch board for active manufacturing work:

- filter active orders
- inspect status, progress, and risk
- identify delayed or paused orders
- prepare start, report, and completion actions

Phase 1 uses scaffolded operational data in the UI; API-backed actions are planned for the next integration phase.

## Equipment

The Equipment page summarizes equipment state and OEE trend readiness. The simulator emits equipment status records and SignalR updates so the API can support near-real-time monitoring.

## Quality Trace

Use Quality Trace to investigate batch history:

- search by batch number
- review work order and operation evidence
- inspect quality inspections and defect patterns
- identify process steps associated with recurring defects

## Knowledge Base

The Knowledge Base page is for SOPs, maintenance manuals, standards, and retrieval readiness. Uploaded documents are parsed into chunks and searched through RAG vector retrieval by the Knowledge Agent.

## Operational Guidance

- Prefer specific questions with dates, equipment IDs, work order codes, or batch numbers.
- Use debug mode when a result looks surprising.
- Treat Phase 1 as shared-data mode; authentication and permissions are out of scope for this phase.
- Do not upload production secrets or private customer data into a shared local environment.

