# C# / .NET 规则

本项目使用 **ASP.NET Core 8.0 + EF Core**，遵循 Clean Architecture 分层。

## 命名约定

- **类名**: PascalCase (`WorkOrder`, `ProductionAgent`)
- **方法**: PascalCase (`GetWorkOrderAsync`, `CalculateOee`)
- **变量/参数**: camelCase (`workOrder`, `batchNumber`)
- **私有字段**: `_camelCase` (`_dbContext`, `_logger`)
- **接口**: `I` 前缀 (`IWorkOrderService`, `IAgentTool`)
- **异步方法**: `Async` 后缀 (`GetByIdAsync`, `ExecuteAsync`)
- **常量**: PascalCase (`MaxRetryCount`, `DefaultTimeout`)

## 依赖注入

所有服务通过构造函数注入，使用接口：

```csharp
public class WorkOrderService : IWorkOrderService
{
    private readonly MesDbContext _context;
    private readonly ILogger<WorkOrderService> _logger;

    public WorkOrderService(
        MesDbContext context,
        ILogger<WorkOrderService> logger)
    {
        _context = context;
        _logger = logger;
    }
}
```

不要使用 Service Locator 模式或直接 new 服务实例。

## 异步编程

- 所有 I/O 操作必须使用 `async/await`
- 避免 `.Result` 或 `.Wait()`，会导致死锁
- 在库代码中使用 `ConfigureAwait(false)`（但 ASP.NET Core 控制器不需要）

```csharp
// Good
var workOrder = await _context.WorkOrders.FindAsync(id);

// Bad - blocks thread
var workOrder = _context.WorkOrders.FindAsync(id).Result;
```

## EF Core 最佳实践

### 只读查询使用 AsNoTracking

```csharp
var workOrders = await _context.WorkOrders
    .AsNoTracking()
    .Where(w => w.Status == WorkOrderStatus.InProgress)
    .ToListAsync();
```

### 使用投影避免加载整个实体

```csharp
// Good - 只查询需要的字段
var summary = await _context.WorkOrders
    .Select(w => new WorkOrderSummary
    {
        Id = w.Id,
        Code = w.Code,
        Progress = (decimal)w.CompletedQuantity / w.PlannedQuantity
    })
    .ToListAsync();

// Bad - 加载整个实体
var orders = await _context.WorkOrders.ToListAsync();
var summary = orders.Select(w => new WorkOrderSummary { ... });
```

### 避免 N+1 查询

```csharp
// Good - 使用 Include 一次性加载
var workOrders = await _context.WorkOrders
    .Include(w => w.Product)
    .Include(w => w.ProductionLine)
    .ToListAsync();

// Bad - N+1 查询
var workOrders = await _context.WorkOrders.ToListAsync();
foreach (var w in workOrders)
{
    var product = await _context.Products.FindAsync(w.ProductId);
}
```

## 错误处理

### 业务异常使用自定义异常

```csharp
public class WorkOrderNotFoundException : Exception
{
    public int WorkOrderId { get; }
    
    public WorkOrderNotFoundException(int workOrderId)
        : base($"Work order {workOrderId} not found")
    {
        WorkOrderId = workOrderId;
    }
}
```

### 在控制器统一处理

```csharp
[HttpGet("{id}")]
public async Task<ActionResult<WorkOrderDto>> GetById(int id)
{
    try
    {
        var result = await _service.GetByIdAsync(id);
        return Ok(result);
    }
    catch (WorkOrderNotFoundException)
    {
        return NotFound();
    }
    catch (InvalidOperationException ex)
    {
        return BadRequest(new { error = ex.Message });
    }
}
```

## Null 安全

- 启用 Nullable Reference Types（已在 Directory.Build.props 配置）
- 使用 `?` 标记可空引用类型
- 使用 `!` 断言非空（谨慎使用）

```csharp
// Navigation property - 运行时保证非空
public Product Product { get; set; } = null!;

// 可选字段
public string? Description { get; set; }

// Null 检查
if (workOrder == null)
    throw new WorkOrderNotFoundException(id);
```

## 现代 C# 特性

### 使用 record 定义 DTO

```csharp
public record WorkOrderDto(
    int Id,
    string Code,
    WorkOrderStatus Status,
    decimal Progress
);
```

### 使用 pattern matching

```csharp
var message = status switch
{
    WorkOrderStatus.NotScheduled => "未排程",
    WorkOrderStatus.InProgress => "生产中",
    WorkOrderStatus.Completed => "已完工",
    _ => "未知状态"
};
```

### 使用 using declaration

```csharp
// Good
public async Task ProcessAsync()
{
    using var scope = _serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MesDbContext>();
    // scope 在方法结束时自动 dispose
}

// Old style
using (var scope = _serviceProvider.CreateScope())
{
    // ...
}
```

## 性能考虑

- 大集合使用 `IAsyncEnumerable<T>` 流式处理
- 避免在循环中调用数据库
- 使用批量操作而非逐条插入
- 复杂查询考虑原始 SQL (`FromSqlRaw`)

## 制造业特定规则

### OEE 计算精度

```csharp
// 使用 decimal 保证精度
public decimal Oee => Availability * Performance * Quality;

// 不要使用 float 或 double
```

### 批次号生成规则

```csharp
// 格式: B{yyyyMMddHHmmss}-{equipmentId}
// 原因：
// 1. B 前缀便于数据库索引和肉眼识别（区分批次号与工单号、产品编码）
// 2. 时间戳精确到秒保证同设备连续批次的唯一性
// 3. 设备 ID 后缀支持快速追溯到生产源头，无需查询数据库
// 4. 格式固定便于正则校验和解析：^B\d{14}-\d+$
var batchNumber = $"B{DateTime.UtcNow:yyyyMMddHHmmss}-{equipmentId}";

// 示例：B20260708143052-102
// 解读：2026年7月8日14:30:52 在设备102上生产
```

### 追溯数据完整性

每条 `ProductionReport` 必须包含：
- BatchNumber（全局唯一）
- WorkOrderId
- ProcessStepId
- EquipmentId
- OperatorId
- Timestamp

不允许任何字段为空。

## 禁止事项

- ❌ 不使用 `dynamic` 类型
- ❌ 不使用 `var` 在类型不明显的地方
- ❌ 不在 Entity 中写业务逻辑（保持贫血模型）
- ❌ 不在控制器中直接访问 DbContext
- ❌ 不手写 SQL 除非性能确实需要
- ❌ 不在循环中使用 `await` 访问数据库（使用批量操作）

### 禁止事项的正确做法

#### ❌ 不使用 `dynamic` 类型

**错误示例**：
```csharp
dynamic payload = await ReadAgentPayloadAsync();
var workOrderCode = payload.workOrder.code; // 运行时才发现字段拼写错误
```

**✅ 正确做法**：
```csharp
public record AgentWorkOrderPayload(WorkOrderDto WorkOrder);

AgentWorkOrderPayload payload = await ReadAgentPayloadAsync<AgentWorkOrderPayload>();
string workOrderCode = payload.WorkOrder.Code;
```

#### ❌ 不使用 `var` 在类型不明显的地方

**错误示例**：
```csharp
var result = await _agentTool.ExecuteAsync();
var data = result.Data;
```

**✅ 正确做法**：
```csharp
FunctionCallResult result = await _agentTool.ExecuteAsync();
WorkOrderSummaryDto data = JObject.FromObject(result.Data)
    .ToObject<WorkOrderSummaryDto>()
    ?? throw new InvalidOperationException("Agent 工具返回结构不符合 WorkOrderSummaryDto。");
```

#### ❌ 不在 Entity 中写业务逻辑（保持贫血模型）

**错误示例**：
```csharp
public class WorkOrder
{
    public int Id { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }

    // 业务逻辑不应在 Entity 中
    public bool IsDelayed() => CompletedQuantity < PlannedQuantity * 0.9m;
}
```

**✅ 正确做法**：
```csharp
// Entity 保持纯数据
public class WorkOrder
{
    public int Id { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }
}

// 业务逻辑在 Service 或 Domain Service 中
public class WorkOrderAnalysisService
{
    public bool IsDelayed(WorkOrder order)
    {
        return order.CompletedQuantity < order.PlannedQuantity * 0.9m;
    }
}
```

#### ❌ 不在控制器中直接访问 DbContext

**错误示例**：
```csharp
[HttpGet]
public async Task<ActionResult> GetWorkOrders()
{
    var orders = await _context.WorkOrders.ToListAsync(); // 违反分层
    return Ok(orders);
}
```

**✅ 正确做法**：
```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetWorkOrders()
{
    IReadOnlyList<WorkOrderDto> orders = await _workOrderService.GetAllAsync();
    return Ok(orders);
}
```

#### ❌ 不手写 SQL 除非性能确实需要

**错误示例**：
```csharp
var status = request.Status;
var sql = $"SELECT * FROM WorkOrders WHERE Status = '{status}'";
var orders = await _context.WorkOrders.FromSqlRaw(sql).ToListAsync();
```

**✅ 正确做法**：
```csharp
var orders = await _context.WorkOrders
    .AsNoTracking()
    .Where(w => w.Status == request.Status)
    .Select(w => new WorkOrderDto(w.Id, w.Code, w.Status, w.Progress))
    .ToListAsync();
```

性能确实需要原始 SQL 时，必须使用参数化查询并记录原因：

```csharp
var orders = await _context.WorkOrders
    .FromSqlInterpolated($"""
        SELECT *
        FROM WorkOrders
        WHERE Status = {request.Status}
    """)
    .AsNoTracking()
    .ToListAsync();
```

#### ❌ 不在循环中使用 `await` 访问数据库（使用批量操作）

**错误示例**：
```csharp
foreach (var orderId in orderIds)
{
    var order = await _context.WorkOrders.FindAsync(orderId); // N+1 查询
    ProcessOrder(order);
}
```

**✅ 正确做法**：
```csharp
// 批量加载后处理
var orders = await _context.WorkOrders
    .Where(w => orderIds.Contains(w.Id))
    .ToListAsync();

foreach (var order in orders)
{
    ProcessOrder(order);
}
```
