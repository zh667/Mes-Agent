# Testing Rules

Use these rules for unit tests, integration tests, frontend tests, E2E tests, and
task acceptance checks.

## Test Pyramid

1. Unit tests: domain rules, services, Agent tool parameter validation, state
   transitions, and pure mappers.
2. Integration tests: API endpoints, EF Core queries, migrations, SignalR, and
   external service adapters with test doubles or containers.
3. E2E tests: critical workflows such as work order reporting, quality tracing,
   OEE dashboard updates, and knowledge-base Q&A.

## .NET Defaults

Preferred libraries:

- xUnit
- Moq or NSubstitute
- FluentAssertions
- WebApplicationFactory
- Testcontainers for PostgreSQL or SQL Server integration tests

Expected commands:

```powershell
dotnet build
dotnet test
dotnet format --verify-no-changes
```

## Frontend Defaults

Preferred libraries:

- Vitest
- Testing Library
- Playwright for E2E

Expected commands when available:

```powershell
pnpm lint
pnpm check:i18n
pnpm typecheck
pnpm test
pnpm build
```

Localization changes must keep `en-US` and `zh-CN` keys identical and
non-empty. Add component coverage for translated labels or interpolation when
behavior changes, and retain an E2E check that locale selection survives a
reload without changing the route.

## MES Test Scenarios

For work order and production flows, test at least:

- Happy path.
- Missing or invalid parameters.
- Entity not found.
- Permission denied.
- Invalid state transition.
- Concurrency conflict.
- External dependency failure.

For quality traceability, test:

- Existing batch with full trace.
- Batch with partial trace.
- Unknown batch.
- Defect aggregation by operation, equipment, and time range.

For Agent tools, test:

- Required parameter validation.
- Authorization boundary.
- Tool result shape.
- No-data result.
- Downstream service failure.
- Audit log creation.

## Example

```csharp
public class WorkOrderServiceTests
{
    [Fact]
    public async Task GetWorkOrderAsync_WhenExists_ReturnsWorkOrder()
    {
        var dbContext = CreateInMemoryDbContext();
        var service = new WorkOrderService(dbContext, Mock.Of<ILogger<WorkOrderService>>());
        var workOrder = new WorkOrder { Id = 1, ProductId = 10 };
        dbContext.WorkOrders.Add(workOrder);
        await dbContext.SaveChangesAsync();

        var result = await service.GetWorkOrderAsync(1);

        result.Should().NotBeNull();
        result.Id.Should().Be(1);
    }
}
```

## Acceptance Reporting

When finishing, state exactly which commands were run. If a check was skipped
because project files do not exist yet, say that directly.
