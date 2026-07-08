# API Contract Rules

Frontend and backend must share API contracts. Request/response schemas and
inferred types must live in a shared contract layer. Frontend code must not
hand-write backend response types.

## Source Of Truth

For this .NET backend plus TypeScript frontend project, the preferred contract
flow is:

1. Define request/response DTOs in a backend-owned contract layer such as
   `src/MesCopilot.Contracts`.
2. Expose those contracts through ASP.NET Core OpenAPI.
3. Generate the TypeScript API client and types into the frontend, for example
   `src/MesCopilot.Web/src/shared/api/generated`.
4. Components and hooks import generated types instead of duplicating DTOs.

If the project later becomes a TypeScript full-stack app, use
`src/shared/contracts` with schema definitions such as Zod, and infer request and
response types from those schemas.

## Contract Change Checklist

- Add or update DTOs in the contract source of truth.
- Add validation rules close to the contract or command handler.
- Update OpenAPI generation or generated frontend client.
- Add or update tests for request validation, response shape, and error shape.
- Update docs and examples when endpoint behavior changes.

## Naming

- Use explicit request and response names:
  - `CreateWorkOrderRequest`
  - `WorkOrderResponse`
  - `TraceQualityRequest`
  - `OeeAnalysisResponse`
- Prefer stable identifiers over display text:
  - `workOrderId`, `batchNumber`, `operationId`, `equipmentId`
- Keep enum names stable across backend, generated client, and persisted data.

## API Compatibility

- Do not remove or rename public fields without a migration plan.
- Additive response fields are usually safe; request field changes need
  validation and compatibility checks.
- Use API versioning only when the product has real consumers that need parallel
  behavior.
- Include pagination, filtering, and sorting contracts for list endpoints before
  lists become large.

## Examples

Backend DTO:

```csharp
public record CreateWorkOrderRequest(
    int ProductId,
    int PlannedQuantity,
    DateTime PlannedStartDate,
    int ProductionLineId
);
```

Frontend usage:

```ts
import type { WorkOrderResponse } from "@/shared/api/generated";

function getWorkOrderLabel(workOrder: WorkOrderResponse) {
  return `${workOrder.id} - ${workOrder.productName}`;
}
```

Do not create a parallel `type WorkOrder = { ... }` in a component for backend
responses.
