# Project Boundaries

## Product Direction

MES Copilot is not a generic Agent platform and not a PLC/SCADA tool. It is a
manufacturing operations copilot that combines MES data, Agent tools, RAG, and
real-time dashboards.

Target users:

- Shift leaders checking work order progress and delays.
- Process engineers investigating production or process issues.
- Quality staff tracing batches, defects, materials, operators, and equipment.
- Production supervisors generating daily reports and reviewing OEE.

Core promise:

> Let manufacturing users ask natural-language questions over MES operations
> data, SOPs, maintenance manuals, and real-time shop-floor signals.

## In Scope

- Work orders, production plans, products, materials, BOMs, routings, operations,
  workstations, production lines, job cards, reporting, and completion.
- Quality inspection, defect records, rework, scrap, batch traceability, and
  exception analysis.
- Equipment status simulation, downtime reasons, alarms, OEE, and SignalR
  push-based dashboards.
- SOP, process document, maintenance manual, and quality standard RAG.
- Agent tools for production progress, quality traceability, OEE analysis, and
  knowledge-base Q&A.

## Out Of Scope For MVP

- Full SCADA implementation.
- Real PLC drivers.
- A complete all-purpose MES clone.
- A generic Dify or AgentX clone.
- Heavy low-code workflow engines.
- Production-grade multi-tenant SaaS operations unless explicitly requested.

## Domain Skeleton

Use mature MES/MOM systems as references instead of inventing business flow from
scratch:

- ISA-95 and MESA-11 for ERP/MES/SCADA boundaries.
- ERPNext Manufacturing for manufacturing document and state flows.
- Odoo Manufacturing for work center, shop-floor, quality, and maintenance
  interaction patterns.
- Siemens Opcenter, SAP Digital Manufacturing, Plex MES, and Tulip for advanced
  business scenarios.

```text
Product / Material / BOM
  -> Routing / Operation / Workstation
  -> Production Plan / Work Order
  -> Dispatch / Job Card / Start / Pause / Report / Complete
  -> Quality Inspection / Defect / Rework / Scrap
  -> Equipment Status / Downtime / OEE
  -> Traceability / Dashboard / Report / Agent Analysis
```

## Status Flows

- Work order: `PendingDispatch -> Dispatched -> InProduction -> Paused -> Completed -> Closed`
- Quality inspection: `Pending -> Passed -> Failed -> Rework | Scrap`
- Equipment: `Running -> Idle -> Down -> Alarm -> Maintenance`

All production records that support traceability must include batch number, work
order number, operation, operator, equipment or workstation, and timestamp.

## Agent Scenarios

- Production progress Agent: work order progress, delay reasons, daily output,
  line load, and shift reports.
- Quality trace Agent: trace work order, operation, personnel, equipment,
  material, and defect patterns by batch.
- OEE Agent: explain low efficiency using running time, downtime, alarms, output,
  and quality rate.
- Knowledge-base Agent: answer SOP, process, maintenance, and exception-handling
  questions with retrieved source context.

## Phased Delivery

1. MES data plus natural-language query: basic tables, query tools, and daily
   report generation. Acceptance example: answer "which work orders are delayed
   today?"
2. Knowledge base plus quality/anomaly analysis: SOP/RAG, maintenance manuals,
   and quality rule explanations. Acceptance example: answer "how should alarm
   A102 be handled?"
3. Real-time dashboard plus equipment simulation plus OEE: Worker Service emits
   equipment status, SignalR pushes updates, and Agent explains OEE changes.
   Acceptance example: dashboard updates equipment state in real time.
