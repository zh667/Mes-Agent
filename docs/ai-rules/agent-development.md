# Agent 开发规则

本项目使用 **BotSharp** 作为 Multi-Agent 框架，开发 4 个领域 Agent。

## Agent 工具规范

每个 Agent 工具必须：
1. 返回 `FunctionCallResult` 包含：
   - `Data`: 结构化数据（JSON）
   - `Explanation`: 自然语言解释
   - `Debug`: 调试信息（仅开发模式）

2. 命名遵循动词开头：`GetTodayWorkOrders`, `AnalyzeDelayedOrders`, `CalculateOee`

3. 提供清晰的 Description 供 LLM 理解

## 工具实现模板

```csharp
public class GetTodayWorkOrdersTool
{
    private readonly IWorkOrderService _service;

    public GetTodayWorkOrdersTool(IWorkOrderService service)
    {
        _service = service;
    }

    public string Name => "GetTodayWorkOrders";
    public string Description => "查询今日工单列表，支持按产线筛选";

    public async Task<FunctionCallResult> ExecuteAsync(
        int? productionLineId = null, 
        bool debugMode = false)
    {
        var sw = Stopwatch.StartNew();
        
        // 1. 调用业务服务
        var workOrders = await _service.GetTodayWorkOrdersAsync(productionLineId);
        var list = workOrders.ToList();
        
        sw.Stop();

        // 2. 构造结构化数据
        var data = new
        {
            workOrders = list.Select(w => new
            {
                w.Id,
                w.Code,
                productName = w.ProductName,
                w.Status,
                w.PlannedQuantity,
                w.CompletedQuantity,
                progress = w.Progress
            }),
            totalCount = list.Count,
            inProgressCount = list.Count(w => w.Status == WorkOrderStatus.InProgress)
        };

        // 3. 生成自然语言解释
        var explanation = $"今天共有 {data.totalCount} 个工单，" +
            $"其中 {data.inProgressCount} 个正在生产。";

        // 4. 返回结果
        return new FunctionCallResult
        {
            Data = data,
            Explanation = explanation,
            Debug = debugMode ? new DebugInfo
            {
                SqlExecuted = "SELECT * FROM WorkOrders WHERE DATE(CreatedAt) = CURRENT_DATE",
                ExecutionTime = $"{sw.ElapsedMilliseconds}ms",
                ToolsCalled = new List<string> { Name }
            } : null
        };
    }
}
```

## 四个 Agent 规范

### 1. ProductionAgent（生产运营）

**职责**：查询工单、分析延期、生成日报

**核心工具**：
- `GetTodayWorkOrdersTool`: 查询今日工单
- `AnalyzeDelayedOrdersTool`: 分析延期原因
- `GenerateDailyReportTool`: 生成生产日报

**典型问题**：
- "今天有哪些工单？"
- "哪些工单延期了？"
- "生成今天的生产日报"

### 2. QualityAgent（质量追溯）

**职责**：批次追溯、不良分析

**核心工具**：
- `TraceBatchTool`: 根据批次号追溯完整链路
- `AnalyzeDefectPatternTool`: 分析不良模式
- `GetDefectsByProcessTool`: 按工序查询不良

**典型问题**：
- "批次 B20260708001 的追溯信息"
- "产品A最近的不良率为什么升高？"
- "工序3有哪些不良记录？"

### 3. OeeAgent（设备效率）

**职责**：计算 OEE、分析低效原因

**核心工具**：
- `CalculateOeeTool`: 计算 OEE（Availability × Performance × Quality）
- `AnalyzeLowOeeTool`: 分析低 OEE 原因
- `GetEquipmentStatusTool`: 查询设备实时状态

**典型问题**：
- "A102 设备今天的 OEE 是多少？"
- "为什么 2 号线 OEE 低？"
- "所有设备的当前状态"

**OEE 计算规则**：
```csharp
public decimal Availability => ActualRunningTime / PlannedProductionTime;
public decimal Performance => (ActualOutput * IdealCycleTime) / ActualRunningTime;
public decimal Quality => (decimal)QualifiedOutput / ActualOutput;
public decimal Oee => Availability * Performance * Quality;
```

### 4. KnowledgeAgent（知识库）

**职责**：SOP/维修手册 RAG 问答

**核心工具**：
- `SearchDocumentsTool`: 向量搜索知识库（RAG）
- `GetSopByCodeTool`: 根据编号获取 SOP
- `AnswerWithContextTool`: 结合 MES 数据回答

**典型问题**：
- "A102 报警怎么处理？"
- "工序3尺寸超差应该怎么办？"
- "这个不良现象可能和哪个工序有关？"

**RAG 流程**：
1. 向量化问题
2. 向量相似度搜索（Top 5）
3. 可选：查询 MES 相关数据
4. 构造 Prompt
5. LLM 生成答案
6. 返回答案 + 文档来源

## Agent 插件结构

```csharp
public class ProductionAgentPlugin
{
    public GetTodayWorkOrdersTool GetTodayWorkOrdersTool { get; }
    public AnalyzeDelayedOrdersTool AnalyzeDelayedOrdersTool { get; }
    public GenerateDailyReportTool GenerateDailyReportTool { get; }

    public ProductionAgentPlugin(IWorkOrderService workOrderService)
    {
        GetTodayWorkOrdersTool = new GetTodayWorkOrdersTool(workOrderService);
        AnalyzeDelayedOrdersTool = new AnalyzeDelayedOrdersTool(workOrderService);
        GenerateDailyReportTool = new GenerateDailyReportTool(workOrderService);
    }

    public string Name => "ProductionAgent";
    public string Description => "生产运营 Agent";
}
```

## 参数验证

所有工具必须验证输入参数：

```csharp
public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, DateTime date)
{
    // 验证参数
    if (equipmentId <= 0)
        throw new ArgumentException("设备ID必须大于0", nameof(equipmentId));
    
    if (date > DateTime.UtcNow.Date)
        throw new ArgumentException("不能查询未来日期", nameof(date));
    
    // 业务逻辑...
}
```

## 错误处理

Agent 工具应该快速验证参数，但**不应该捕获业务异常**，让上层统一处理：

```csharp
public async Task<FunctionCallResult> ExecuteAsync(int equipmentId, DateTime date, bool debugMode = false)
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
        Debug = debugMode ? new DebugInfo
        {
            ExecutionTime = "120ms",
            ToolsCalled = new List<string> { "CalculateOee" }
        } : null
    };
}
```

**异常处理分层**：
- **Agent 工具层**：参数验证，抛出 `ArgumentException`
- **业务服务层**：业务规则验证，抛出自定义异常（例如 `WorkOrderNotFoundException`）
- **BotSharp 中间件/全局过滤器**：捕获所有异常，转换为用户友好的 `FunctionCallResult`

**全局异常处理示例**：

```csharp
public class AgentToolExceptionMiddleware
{
    private readonly ILogger<AgentToolExceptionMiddleware> _logger;

    public AgentToolExceptionMiddleware(ILogger<AgentToolExceptionMiddleware> logger)
    {
        _logger = logger;
    }

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

## 测试要求

每个 Agent 工具必须有单元测试：

```csharp
[Fact]
public async Task GetTodayWorkOrders_WithNoOrders_ShouldReturnEmptyList()
{
    // Arrange
    var mockService = new Mock<IWorkOrderService>();
    mockService.Setup(s => s.GetTodayWorkOrdersAsync(null))
        .ReturnsAsync(new List<WorkOrderDto>());
    
    var tool = new GetTodayWorkOrdersTool(mockService.Object);
    
    // Act
    var result = await tool.ExecuteAsync();
    
    // Assert
    Assert.NotNull(result.Data);
    var data = JObject.FromObject(result.Data);
    Assert.Equal(0, data["totalCount"]);
}
```

## 集成测试要求

每个 Agent 工具需要集成测试验证完整流程：

```csharp
public class CalculateOeeToolIntegrationTests : IClassFixture<MesWebApplicationFactory>
{
    private readonly MesWebApplicationFactory _factory;

    public CalculateOeeToolIntegrationTests(MesWebApplicationFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CalculateOee_WithRealData_ShouldReturnCorrectMetrics()
    {
        // Arrange - 使用 WebApplicationFactory + 测试数据库
        await using var scope = _factory.Services.CreateAsyncScope();
        var tool = scope.ServiceProvider.GetRequiredService<CalculateOeeTool>();

        await SeedTestDataAsync(scope.ServiceProvider);

        // Act
        var result = await tool.ExecuteAsync(equipmentId: 1, DateTime.Today);

        // Assert
        Assert.NotNull(result.Data);
        var oee = JObject.FromObject(result.Data)["oee"]!.Value<decimal>();
        Assert.InRange(oee, 0m, 1m); // OEE 必须在 [0, 1] 区间
    }
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

## 性能要求

- Agent 工具响应时间 < 3 秒（不含 LLM 调用）
- 数据库查询 < 500ms
- 向量搜索 < 200ms
- 使用 `Stopwatch` 记录执行时间到 Debug 信息

## 性能测试

使用 BenchmarkDotNet 验证性能要求：

```csharp
[MemoryDiagnoser]
public class AgentToolBenchmarks
{
    private GetTodayWorkOrdersTool _tool = null!;
    private MesWebApplicationFactory _factory = null!;
    private AsyncServiceScope _scope;

    [GlobalSetup]
    public void Setup()
    {
        _factory = new MesWebApplicationFactory();
        _scope = _factory.Services.CreateAsyncScope();
        _tool = _scope.ServiceProvider.GetRequiredService<GetTodayWorkOrdersTool>();
    }

    [Benchmark]
    public async Task<FunctionCallResult> GetTodayWorkOrders_Performance()
    {
        var result = await _tool.ExecuteAsync();
        return result;
    }

    [GlobalCleanup]
    public async Task Cleanup()
    {
        await _scope.DisposeAsync();
        await _factory.DisposeAsync();
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

## 调试模式

开发模式下，启用 `debugMode` 参数显示：
- 执行的 SQL
- 执行时间
- 调用的工具链
- 数据来源

生产模式下，`Debug` 字段为 `null`。
