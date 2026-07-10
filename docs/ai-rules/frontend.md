# Frontend Rules

Use these rules for React, Blazor, dashboards, and shop-floor interaction
surfaces.

## Product Fit

MES Copilot is an operational tool. The UI should be dense, scannable, and calm:

- Prioritize tables, filters, status chips, timelines, detail drawers, and
  dashboards over marketing-style pages.
- Make common workflows efficient for repeated daily use.
- Keep typography compact in tool surfaces.
- Show manufacturing status clearly: work order state, equipment state, quality
  result, delay reason, and batch trace.

## React Structure

When using React, prefer feature-oriented folders:

```text
src/MesCopilot.Web/src/
  app/
  shared/
    api/generated/
    components/
    hooks/
    utils/
  features/
    work-orders/
    quality-trace/
    equipment-oee/
    knowledge-base/
```

Rules:

- Components call typed hooks or service functions, not raw `fetch`.
- Keep generated API code under `shared/api/generated`.
- Keep UI-only mappers separate from generated DTOs.
- Use tables with stable columns for operational lists.
- Use detail panels for work order, batch, and equipment drill-downs.

## Blazor Structure

When using Blazor:

- Keep pages thin and move business calls into injectable services.
- Use strongly typed models generated from or aligned with backend contracts.
- Keep reusable UI in shared components.
- Avoid mixing data access and UI rendering logic in `.razor` files.

## SignalR

- Put SignalR connection logic in a dedicated hook/service.
- Reconnect with backoff and show stale/offline state.
- Treat real-time events as patches to typed server state, not as the only source
  of truth.
- Name client events in camelCase, for example `workOrderUpdated` and
  `equipmentStatusChanged`.

## Accessibility

- Controls need visible labels or accessible names.
- Status colors require text labels too.
- Keyboard navigation must work for core forms and tables.
- Do not rely on color alone for quality or equipment states.

## Localization

- English (`en-US`) and Simplified Chinese (`zh-CN`) are supported product
  locales. Every user-visible string must be added to both catalogs in the same
  change.
- Use `useTranslations` in client components and the corresponding `next-intl`
  server API in server components. Do not hardcode headings, buttons, labels,
  placeholders, empty states, errors, status text, tooltips, `alt`, or
  `aria-label` values.
- Keep message keys grouped by feature and describe intent, not the English
  wording. Prefer `scheduling.adjustError` over `messages.error2`.
- Use ICU-style placeholders for dynamic values. Do not assemble translated
  sentences from fragments or interpolate raw HTML.
- Do not translate business data, identifiers, protocol names, product names,
  operator-entered text, or backend/Agent output unless the API explicitly
  provides localized values.
- When a technical identifier must remain as literal visible JSX, add a nearby
  `i18n-ignore: <reason>` comment. Baseline suppressions and reason-free ignores
  are not allowed.
- Changing locale must preserve the current route and user workflow. Format
  dates and numbers with the active locale.

## Validation

Run frontend checks when available:

```powershell
pnpm lint
pnpm check:i18n
pnpm typecheck
pnpm test
pnpm build
```
