# 工程规则改进任务清单

基于 clean-code-reviewer 审查结果，需要对以下文件进行改进：

- `docs/ai-rules/csharp-dotnet.md`
- `docs/ai-rules/agent-development.md`

---

## 任务 1：补充集成测试指导

**文件**: `docs/ai-rules/agent-development.md`

**位置**: 在"## 测试要求"章节（当前 line 214）之后补充

**新增内容**:

```markdown
## 集成测试要求

每个 Agent 工具需要集成测试验证完整流程：

```csharp
[Fact]
public async Task CalculateOee_WithRealData_ShouldReturnCorrectMetrics()
{
    // Arrange - 使用 WebApplicationFactory + 测试数据库
    await using var factory = new MesWebApplicationFactory();
    var scope = factory.Services.CreateScope();
    var tool = scope.ServiceProvider.GetRequiredService<CalculateOeeTool>();
    
    await SeedTestDataAsync(scope); // 准备测试工单、设备状态
    
    // Act
    var result = await tool.ExecuteAsync(equipmentId: 1, DateTime.Today);
    
    // Assert
    Assert.NotNull(result.Data);
    var oee = JObject.FromObject(result.Data)["oee"].Value<decimal>();
    Assert.InRange(oee, 0m, 1m); // OEE 必须在 [0, 1] 区间
}
```

**边界条件测试必须覆盖**：
- 空结果集（今天无工单）
- 无效参数（负数 ID、未来日期）
- 数据不完整（工单缺设备记录）
- RAG 无匹配文档

**测试隔离**：
- 每个测试使用独立的测试数据库实例
- 测试结束后清理数据
- 使用 `IClassFixture<MesWebApplicationFactory>` 共享测试上下文
```

---

## 任务 2：补充性能测试指导

**文件**: `docs/ai-rules/agent-development.md`

**位置**: 在"## 性能要求"章节（当前 line 239）之后补充

**新增内容**:

```markdown
## 性能测试

使用 BenchmarkDotNet 验证性能要求：

```csharp
[MemoryDiagnoser]
public class AgentToolBenchmarks
{
    private GetTodayWorkOrdersTool _tool;
    private IServiceScope _scope;
    
    [GlobalSetup]
    public void Setup()
    {
        var factory = new MesWebApplicationFactory();
        _scope = factory.Services.CreateScope();
        _tool = _scope.ServiceProvider.GetRequiredService<GetTodayWorkOrdersTool>();
    }
    
    [Benchmark]
    public async Task<FunctionCallResult> GetTodayWorkOrders_Performance()
    {
        var result = await _tool.ExecuteAsync();
        return result;
    }
    
    [GlobalCleanup]
    public void Cleanup()
    {
        _scope?.Dispose();
    }
}
```

**验收标准**：
- P95 延迟 < 3 秒（不含 LLM 调用）
- 数据库查询 < 500ms（通过 EF Core Logging 或 MiniProfiler 验证）
- 向量搜索 < 200ms（pgvector `EXPLAIN ANALYZE` 验证）

**性能监控**：
- 在 `Debug` 字段中包含 `ExecutionTime`
- 使用 Application Insights 或 Prometheus 记录 Agent 工具调用时长
- 在 CI/CD 中运行基准测试，阻止性能退化
```

---

## 任务 3：修正 Agent 错误处理逻辑

**文件**: `docs/ai-rules/agent-development.md`

**位置**: 替换"## 错误处理"章节（当前 line 194-212）

**替换为**:

```markdown
## 错误处理

Agent 工具应该快速验证参数，但**不应该捕获业务异常** — 让上层统一处理：

```csharp
public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, DateTime date)
{
    // 1. 参数验证 - 快速失败
    ArgumentOutOfRangeException.ThrowIfNegativeOrZero(equipmentId);
    if (date > DateTime.UtcNow.Date)
        throw new ArgumentException("不能查询未来日期", nameof(date));
    
    // 2. 调用服务 - 异常向上传播，不在此捕获
    var oee = await _service.CalculateOeeAsync(equipmentId, date);
    
    // 3. 构造返回结果
    return new FunctionCallResult 
    { 
        Data = oee, 
        Explanation = $"设备 {equipmentId} 在 {date:yyyy-MM-dd} 的 OEE 为 {oee.Value:P1}",
        Debug = debugMode ? new DebugInfo { ... } : null
    };
}
```

**异常处理分层**：
- **Agent 工具层**：参数验证，抛出 `ArgumentException`
- **业务服务层**：业务规则验证，抛出自定义异常（`WorkOrderNotFoundException`）
- **BotSharp 中间件/全局过滤器**：捕获所有异常，转换为用户友好的 `FunctionCallResult`

**全局异常处理示例**：
```csharp
public class AgentToolExceptionMiddleware
{
    public async Task<FunctionCallResult> HandleAsync(Func<Task<FunctionCallResult>> next)
    {
        try
        {
            return await next();
        }
        catch (ArgumentException ex)
        {
            return new FunctionCallResult
            {
                Data = null,
                Explanation = $"参数错误：{ex.Message}"
            };
        }
        catch (NotFoundException ex)
        {
            return new FunctionCallResult
            {
                Data = null,
                Explanation = $"未找到数据：{ex.Message}"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Agent 工具执行失败");
            return new FunctionCallResult
            {
                Data = null,
                Explanation = "抱歉，系统遇到问题，请稍后重试。"
            };
        }
    }
}
```
```

---

## 任务 4：补充批次号生成的业务原因

**文件**: `docs/ai-rules/csharp-dotnet.md`

**位置**: 替换"### 批次号生成规则"章节（当前 line 215-220）

**替换为**:

```markdown
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
```

---

## 任务 5：补充"禁止事项"的替代方案

**文件**: `docs/ai-rules/csharp-dotnet.md`

**位置**: 在"## 禁止事项"章节（当前 line 234-242）末尾补充

**新增内容**:

```markdown
### 禁止事项的正确做法

#### ❌ 循环中 await 数据库

**错误示例**：
```csharp
foreach (var orderId in orderIds)
{
    var order = await _context.WorkOrders.FindAsync(orderId); // N+1 查询
    ProcessOrder(order);
}
```

**正确做法**：
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

#### ❌ 控制器直接访问 DbContext

**错误示例**：
```csharp
[HttpGet]
public async Task<ActionResult> GetWorkOrders()
{
    var orders = await _context.WorkOrders.ToListAsync(); // 违反分层
    return Ok(orders);
}
```

**正确做法**：
```csharp
[HttpGet]
public async Task<ActionResult<IEnumerable<WorkOrderDto>>> GetWorkOrders()
{
    var orders = await _workOrderService.GetAllAsync(); // 通过服务层
    return Ok(orders);
}
```

#### ❌ Entity 中写业务逻辑（贫血模型原则）

**错误示例**：
```csharp
public class WorkOrder
{
    public int Id { get; set; }
    public decimal PlannedQuantity { get; set; }
    public decimal CompletedQuantity { get; set; }
    
    // ❌ 业务逻辑不应在 Entity 中
    public bool IsDelayed() => CompletedQuantity < PlannedQuantity * 0.9m;
}
```

**正确做法**：
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
```

---

## 验收标准

所有改进完成后：

1. ✅ 每个章节都有完整的代码示例
2. ✅ "禁止事项"都有对应的"正确做法"
3. ✅ 测试覆盖：单元测试 + 集成测试 + 性能测试
4. ✅ 制造业特定规则都说明了"为什么"
5. ✅ Agent 错误处理逻辑与服务层错误处理不冲突

## 提交信息

```
docs: improve engineering rules based on clean-code-reviewer feedback

- Add integration test and performance test guidelines
- Fix Agent error handling logic (don't catch business exceptions)
- Add rationale for manufacturing-specific rules (batch number format)
- Supplement correct alternatives for "Don'ts" section
- Ensure all prohibitions have corresponding best practices
```
