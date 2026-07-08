# Backend Rules

Use these rules for ASP.NET Core, C#, Web API, SignalR, Worker Service, and Agent
tool implementation.

## Architecture

Expected project boundaries:

```text
src/
  MesCopilot.Api/              # ASP.NET Core Web API, controllers, hubs
  MesCopilot.Core/             # domain models, domain services, business rules
  MesCopilot.Infrastructure/   # EF Core, repositories, external services
  MesCopilot.Agent/            # Agent definitions, tools, RAG orchestration
  MesCopilot.Contracts/        # request/response contracts
  MesCopilot.Worker/           # equipment simulation and background jobs
```

Rules:

- Controllers stay thin. Move business logic into application/domain services.
- Infrastructure depends inward on Core and Contracts; Core does not depend on
  Infrastructure.
- Keep Agent tool implementations separate from HTTP controllers.
- Prefer constructor dependency injection and interfaces for services.

## C# Style

- Classes, records, methods: PascalCase.
- Variables and parameters: camelCase.
- Private fields: `_camelCase`.
- Interfaces: `IWorkOrderService`, `IAgentTool`.
- Async methods end with `Async`.
- Prefer records for immutable request/response shapes.
- Avoid `.Result` and `.Wait()` on tasks.

Example:

```csharp
public class WorkOrderService : IWorkOrderService
{
    private readonly AppDbContext _dbContext;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(
        AppDbContext dbContext,
        ILogger<WorkOrderService> logger)
    {
        _dbContext = dbContext;
        _logger = logger;
    }
}
```

## REST API

Use resource-oriented routes:

```text
GET    /api/workorders
GET    /api/workorders/{id}
POST   /api/workorders
PUT    /api/workorders/{id}
DELETE /api/workorders/{id}

POST   /api/workorders/{id}/start
POST   /api/workorders/{id}/pause
POST   /api/workorders/{id}/report
POST   /api/workorders/{id}/complete
```

Rules:

- Do not expose EF entities directly.
- Use DTOs from the contract layer.
- Validate requests before executing business operations.
- Add pagination for list endpoints expected to grow.
- Return consistent problem details for validation and domain failures.

## SignalR

- Put hubs in `MesCopilot.Api/Hubs`.
- Use PascalCase for hub methods.
- Use camelCase for client event names.
- Include enough identifiers in events for clients to update cached state.

Example:

```csharp
public class ProductionHub : Hub
{
    public async Task SubscribeToLine(int lineId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"line-{lineId}");
    }
}
```

## Agent Tools

Each Agent tool should be a small C# class with explicit input validation,
auditing, and a narrow purpose.

Preferred tool names:

- `GetWorkOrderProgress`
- `TraceQuality`
- `AnalyzeOee`
- `SearchSopKnowledge`

Rules:

- Name tools with a verb and a specific domain scope.
- Validate all parameters.
- Return structured data when possible; let the Agent summarize it.
- Record audit logs for tool calls.
- Do not let an Agent bypass authorization or domain rules.

Example:

```csharp
public async Task<string> ExecuteAsync(Dictionary<string, object> parameters)
{
    if (!parameters.TryGetValue("workOrderId", out var idObj))
        throw new ArgumentException("workOrderId is required");

    if (!int.TryParse(idObj.ToString(), out var workOrderId))
        throw new ArgumentException("workOrderId must be a valid integer");

    // Query domain service here.
}
```
