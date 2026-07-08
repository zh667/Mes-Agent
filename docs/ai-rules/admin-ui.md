# Admin UI Rules

Use these rules for MES operations screens, dashboards, admin panels, and
shop-floor tools.

## Interface Priorities

- Build the actual workflow first, not a landing page.
- Optimize for scanning, comparison, filtering, and repeated actions.
- Make the first viewport useful: key metrics, active exceptions, current work
  order status, or the primary table.
- Avoid decorative layouts that make production status harder to inspect.

## Common Screens

- Work order list: status, product, plan quantity, completed quantity, line,
  due time, delay risk, owner, and actions.
- Work order detail: routing, operations, job cards, material usage, quality
  records, equipment records, and Agent summary.
- Quality trace: batch search, trace graph/table, defect distribution, and
  source records.
- Equipment/OEE: current status, downtime timeline, alarms, OEE components, and
  explanation panel.
- Knowledge base: documents, chunks, source metadata, re-index status, and
  retrieval preview.

## Interaction Patterns

- Use filters for status, date, production line, product, batch, and equipment.
- Use side panels or detail pages for drill-downs.
- Use confirmations for destructive or irreversible actions.
- Use inline validation for forms.
- Use loading, empty, error, and stale data states.
- Keep action buttons close to the entity they affect.

## Status Presentation

Always pair status color with text:

- Work order: pending dispatch, dispatched, in production, paused, completed,
  closed.
- Quality: pending, passed, failed, rework, scrap.
- Equipment: running, idle, down, alarm, maintenance.

## Agent UX

- Show which data sources or tools the Agent used when possible.
- For operational recommendations, show the underlying records or retrieved
  documents.
- Do not let Agent text replace structured status, validation, or audit logs.
- For high-impact actions, require user confirmation after Agent suggestions.
