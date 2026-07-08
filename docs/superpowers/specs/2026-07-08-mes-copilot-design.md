# MES Copilot 设计文档

**项目名称**: MES Copilot - 面向制造现场的智能运营 Agent 平台  
**创建日期**: 2026-07-08  
**设计版本**: v1.0  
**目标**: 让班组长、工艺员、质量人员、生产主管用自然语言查询 MES 数据、分析异常、追溯质量、生成日报

---

## 1. 项目概述

### 1.1 核心定位

MES Copilot 是一个结合制造执行系统（MES）和 AI Agent 技术的智能运营平台，通过自然语言交互方式，帮助制造现场人员快速获取生产数据、分析异常、追溯质量问题，并基于知识库（SOP/维修手册）给出处理建议。

### 1.2 技术架构选型

**实施方案**: 单仓库融合架构（方案 A）

**技术栈**:
- **后端**: ASP.NET Core 8.0 Web API
- **ORM**: Entity Framework Core 8.0
- **数据库**: PostgreSQL 16 + pgvector 扩展
- **Agent 框架**: BotSharp（深度集成，作为 NuGet 包引入）
- **实时通信**: SignalR
- **后台任务**: Worker Service
- **文档解析**: iTextSharp (PDF)、DocumentFormat.OpenXml (Word/Excel)
- **前端**: Next.js 14 (App Router) + React 18
- **UI 组件**: shadcn/ui
- **状态管理**: TanStack Query
- **测试**: xUnit + Moq + FluentAssertions + Testcontainers

### 1.3 参考架构

基于以下开源项目进行二次开发:
- **MES 底座**: yuwang/MES (.NET 8 + React + MySQL + EF Core + SignalR)
- **Agent 底座**: BotSharp (C#/.NET Multi-Agent Framework)
- **业务参考**: ISA-95、ERPNext Manufacturing、Odoo Manufacturing

---

## 2. 分期规划

### 第一期: MVP - 四个 Agent 基础功能（当前设计）

**目标**: 跑通完整流程，验证技术可行性

**包含功能**:
- ✅ 完整 MES 数据模型（产品、BOM、工单、工序、设备、质检、文档）
- ✅ 四个 Agent 基础工具
  - 生产运营 Agent: 查询工单、分析延期、生成日报
  - 质量追溯 Agent: 批次追溯、不良模式分析
  - OEE 分析 Agent: 计算 OEE、分析低效原因
  - 知识库 Agent: RAG 问答、结合 MES 数据回答
- ✅ 设备数据半真实模拟（根据工单状态生成合理数据）
- ✅ SignalR 实时推送设备状态
- ✅ 多格式文档上传与向量化（PDF/Word/Excel）
- ✅ React 前端（Agent 对话页面 + 调试模式）
- ✅ Agent 返回结构化数据 + 自然语言解释
- ✅ 单元测试 + 集成测试 + Agent 输出验证测试

**不包含**:
- ❌ 用户认证和权限管理（所有用户共享数据）
- ❌ 多租户支持
- ❌ Agent 响应的事实性验证（可能产生幻觉）
- ❌ 完整的 BOM 展开和工艺路线自动排程
- ❌ 真实 PLC 连接

**验收标准**: 见第 8 节

### 第二期: 增强与完善

**目标**: 提升可用性和准确性

**计划功能**:
- 用户登录和角色权限（管理员、班组长、质检员、操作工）
- 完善 Agent 提示词工程，减少幻觉和错误回答
- 增加更多 Agent 工具
  - 设备预测性维护建议
  - 生产排程优化建议
  - 异常根因分析（5-Why）
- 报表导出功能（Excel/PDF）
- Agent 对话历史检索和复用
- 知识库文档版本管理

### 第三期: 企业级能力

**目标**: 达到生产可用标准

**计划功能**:
- 多租户支持（SaaS 模式）
- 完整的 BOM 展开和工艺路线自动排程
- 真实 PLC 连接（OPC UA/Modbus TCP）
- Agent 事实性验证机制（对比数据库真实数据）
- 审计日志和合规性报告
- 性能优化（缓存、分库分表）
- 移动端 App（React Native）

---

## 3. 项目结构


```
MesCopilot/
├── src/
│   ├── MesCopilot.Domain/                    # 领域模型层
│   │   ├── Entities/                         # 实体（参考 yuwang/MES）
│   │   │   ├── Products/                     # 产品、BOM
│   │   │   ├── Production/                   # 工单、工序、报工
│   │   │   ├── Quality/                      # 质检、不良记录
│   │   │   ├── Equipment/                    # 设备、状态、报警
│   │   │   └── Knowledge/                    # 知识库文档
│   │   ├── Enums/                           # 枚举（工单状态、设备状态等）
│   │   └── ValueObjects/                    # 值对象
│   │
│   ├── MesCopilot.Infrastructure/           # 基础设施层
│   │   ├── Data/                            # EF Core DbContext
│   │   ├── Repositories/                    # 数据访问
│   │   ├── VectorStore/                     # 向量数据库（pgvector/Qdrant）
│   │   └── DocumentParsers/                 # PDF/Word/Excel 解析器
│   │
│   ├── MesCopilot.Application/              # 应用服务层
│   │   ├── Services/                        # MES 业务服务
│   │   │   ├── IWorkOrderService
│   │   │   ├── IQualityService
│   │   │   ├── IEquipmentService
│   │   │   └── IKnowledgeService
│   │   └── Dtos/                           # 数据传输对象
│   │
│   ├── MesCopilot.Agent/                    # Agent 层（BotSharp 集成）
│   │   ├── Plugins/                         # BotSharp 插件
│   │   │   ├── ProductionAgentPlugin/       # 生产运营 Agent
│   │   │   ├── QualityAgentPlugin/          # 质量追溯 Agent
│   │   │   ├── OeeAgentPlugin/              # OEE 分析 Agent
│   │   │   └── KnowledgeAgentPlugin/        # 知识库 Agent
│   │   ├── Tools/                           # Agent 工具定义
│   │   └── Models/                          # Agent 响应模型
│   │
│   ├── MesCopilot.Api/                      # Web API 入口
│   │   ├── Controllers/                     # REST Controllers
│   │   │   ├── WorkOrdersController         # MES API
│   │   │   ├── EquipmentController
│   │   │   └── AgentController              # Agent 对话 API
│   │   ├── Hubs/                           # SignalR Hubs
│   │   └── Program.cs                       # 启动配置
│   │
│   └── MesCopilot.DeviceSimulator/          # 设备模拟器（Worker Service）
│       └── Workers/
│           └── EquipmentSimulatorWorker.cs
│
├── web/                                     # Next.js 前端
│   ├── app/                                # App Router
│   ├── components/                         # React 组件
│   └── lib/                                # API 客户端
│
└── tests/
    ├── MesCopilot.UnitTests/
    └── MesCopilot.IntegrationTests/
```

---

## 4. 数据模型设计

### 4.1 核心实体（基于 yuwang/MES + ISA-95）

#### 产品管理
- **Product**: 产品主数据
- **Material**: 物料主数据
- **Bom**: 物料清单
- **BomItem**: BOM 明细

#### 工艺路线
- **ProcessRoute**: 工艺路线
- **ProcessStep**: 工序定义
- **Workstation**: 工位/工作中心

#### 生产执行
- **WorkOrder**: 生产工单
- **WorkOrderOperation**: 工单工序
- **ProductionReport**: 报工记录
- **ProductionLine**: 生产线

#### 质量管理
- **QualityInspection**: 质检记录
- **DefectRecord**: 不良记录
- **DefectType**: 不良类型

#### 设备管理
- **Equipment**: 设备主数据
- **EquipmentStatus**: 设备状态记录
- **EquipmentAlarm**: 设备报警
- **DowntimeRecord**: 停机记录

#### 知识库
- **Document**: 文档（SOP/维修手册）
- **DocumentChunk**: 文档切片（用于 RAG）
- **DocumentType**: 文档类型（枚举: SOP/维修手册/工艺文件/异常处理规范）

### 4.2 关键状态流转

```csharp
// 工单状态
public enum WorkOrderStatus
{
    NotScheduled,    // 未排程
    Scheduled,       // 已排程
    InProgress,      // 生产中
    Paused,          // 暂停
    Completed,       // 完工
    Closed           // 关闭
}

// 质检状态
public enum InspectionStatus
{
    Pending,         // 待检
    Pass,            // 合格
    Fail             // 不合格（需返工或报废）
}

// 设备状态
public enum EquipmentState
{
    Running,         // 运行
    Idle,            // 待机
    Alarm,           // 报警
    Maintenance,     // 维修
    Offline          // 离线
}
```

### 4.3 追溯链路设计

每条生产记录必须包含以下字段以支持质量追溯:

```csharp
public class ProductionReport
{
    public string BatchNumber { get; set; }        // 批次号（全局唯一）
    public int WorkOrderId { get; set; }           // 工单
    public int ProcessStepId { get; set; }         // 工序
    public int EquipmentId { get; set; }           // 设备
    public int OperatorId { get; set; }            // 操作人员
    public DateTime Timestamp { get; set; }        // 时间戳
    public int Quantity { get; set; }              // 数量
    public int QualifiedQuantity { get; set; }     // 合格数量
}
```

### 4.4 OEE 计算数据

```csharp
// OEE = Availability × Performance × Quality
public class OeeCalculationData
{
    public TimeSpan PlannedProductionTime { get; set; }    // 计划生产时间
    public TimeSpan ActualRunningTime { get; set; }        // 实际运行时间
    public TimeSpan IdealCycleTime { get; set; }          // 理想节拍
    public int ActualOutput { get; set; }                  // 实际产量
    public int QualifiedOutput { get; set; }               // 合格产量
    
    public decimal Availability => ActualRunningTime / PlannedProductionTime;
    public decimal Performance => (ActualOutput * IdealCycleTime) / ActualRunningTime;
    public decimal Quality => (decimal)QualifiedOutput / ActualOutput;
    public decimal Oee => Availability * Performance * Quality;
}
```

---

## 5. Agent 工具设计

### 5.1 Agent 工具接口规范

每个工具实现 BotSharp 的 `IAgentFunction` 接口:

```csharp
public interface IAgentFunction
{
    string Name { get; }
    string Description { get; }
    JObject Parameters { get; }  // JSON Schema
    Task<FunctionCallResult> Execute(FunctionCallContext context);
}

public class FunctionCallResult
{
    public object Data { get; set; }              // 结构化数据（JSON）
    public string Explanation { get; set; }       // 自然语言解释
    public DebugInfo Debug { get; set; }          // 调试信息（仅开发模式）
}

public class DebugInfo
{
    public string SqlExecuted { get; set; }
    public string ExecutionTime { get; set; }
    public string DataSource { get; set; }
    public List<string> ToolsCalled { get; set; }
}
```


### 5.2 生产运营 Agent (ProductionAgentPlugin)

#### 工具 1: GetTodayWorkOrders
- **功能**: 查询今日工单列表
- **输入参数**:
  - `productionLineId` (可选): 产线 ID
  - `status` (可选): 工单状态
- **输出示例**:
```json
{
  "data": {
    "workOrders": [
      {
        "id": 1001,
        "productName": "产品A",
        "status": "InProgress",
        "plannedQuantity": 1000,
        "completedQuantity": 750,
        "progress": 0.75
      }
    ],
    "totalCount": 12,
    "inProgressCount": 8,
    "completedCount": 4
  },
  "explanation": "今天共有 12 个工单，其中 8 个正在生产，4 个已完工。",
  "debug": {
    "sqlExecuted": "SELECT * FROM WorkOrders WHERE DATE(CreatedAt) = CURRENT_DATE",
    "executionTime": "45ms"
  }
}
```

#### 工具 2: AnalyzeDelayedOrders
- **功能**: 分析延期工单
- **输入参数**:
  - `startDate`: 开始日期
  - `endDate`: 结束日期
- **输出**: 延期工单列表 + 延期原因统计（物料短缺、设备故障、人员不足等）

#### 工具 3: GenerateDailyReport
- **功能**: 生成生产日报
- **输入参数**:
  - `date`: 日期
  - `productionLineId` (可选): 产线 ID
- **输出**: JSON（产量、完工率、异常汇总、OEE） + 格式化文字报告

### 5.3 质量追溯 Agent (QualityAgentPlugin)

#### 工具 1: TraceBatch
- **功能**: 批次追溯
- **输入参数**:
  - `batchNumber`: 批次号
- **输出示例**:
```json
{
  "data": {
    "batch": "B20260708001",
    "workOrder": { "id": 1001, "productName": "产品A" },
    "processSteps": [
      {
        "stepName": "冲压",
        "equipment": { "id": "A102", "name": "冲压机2号" },
        "operator": { "id": 501, "name": "张三" },
        "startTime": "2026-07-08 08:30:00",
        "endTime": "2026-07-08 09:15:00"
      }
    ],
    "materials": [
      { "name": "钢板", "batchNumber": "M20260701" }
    ],
    "qualityInspections": [
      { "result": "Pass", "inspector": "李四" }
    ]
  },
  "explanation": "批次 B20260708001 追溯结果：工单 WO001，经过 3 道工序..."
}
```

#### 工具 2: AnalyzeDefectPattern
- **功能**: 分析不良模式
- **输入参数**:
  - `productId`: 产品 ID
  - `startDate`: 开始日期
  - `endDate`: 结束日期
- **输出**: 不良类型分布、工序分布、时间趋势图数据 + 分析结论

#### 工具 3: GetDefectsByProcess
- **功能**: 按工序查询不良记录
- **输入参数**:
  - `processStepId`: 工序 ID
  - `startDate`: 开始日期
  - `endDate`: 结束日期
- **输出**: 不良记录列表 + 汇总统计

### 5.4 OEE 分析 Agent (OeeAgentPlugin)

#### 工具 1: CalculateOee
- **功能**: 计算 OEE
- **输入参数**:
  - `equipmentId` 或 `productionLineId`
  - `startDate`: 开始日期
  - `endDate`: 结束日期
- **输出示例**:
```json
{
  "data": {
    "oee": 0.72,
    "availability": 0.85,
    "performance": 0.90,
    "quality": 0.94,
    "plannedTime": "8h",
    "runningTime": "6.8h",
    "downtimeMinutes": 72
  },
  "explanation": "设备 A102 在 2026-07-08 的 OEE 为 72%，主要损失在可用率..."
}
```

#### 工具 2: AnalyzeLowOee
- **功能**: 分析低 OEE 原因
- **输入参数**:
  - `equipmentId`: 设备 ID
  - `date`: 日期
- **输出**: 停机记录、报警记录、产量数据 + 诊断结论（如"频繁报警导致 OEE 下降"）

#### 工具 3: GetEquipmentStatus
- **功能**: 查询设备状态
- **输入参数**:
  - `equipmentIds`: 设备 ID 列表
- **输出**: 实时状态、最近报警、运行时长 + 状态说明

### 5.5 知识库 Agent (KnowledgeAgentPlugin)

#### 工具 1: SearchDocuments
- **功能**: 搜索知识库（RAG）
- **输入参数**:
  - `query`: 查询文本
  - `documentType` (可选): 文档类型（SOP/维修手册/工艺文件）
  - `topK`: 返回结果数（默认 5）
- **输出示例**:
```json
{
  "data": {
    "results": [
      {
        "documentName": "设备维修手册",
        "chunkText": "A102 报警代码 E001 表示传感器故障，处理步骤...",
        "page": 23,
        "similarity": 0.89
      }
    ]
  },
  "explanation": "根据维修手册第 23 页，A102 报警 E001 的处理方法是...",
  "debug": {
    "vectorSearchTime": "120ms",
    "llmCallTime": "1.5s"
  }
}
```

#### 工具 2: GetSopByCode
- **功能**: 根据编号获取 SOP
- **输入参数**:
  - `sopCode`: SOP 编号
- **输出**: SOP 内容 + 格式化文本

#### 工具 3: AnswerWithContext
- **功能**: 结合 MES 数据回答
- **输入参数**:
  - `question`: 问题
  - `equipmentId` 或 `processStepId` (可选): 上下文
- **输出**: 知识库答案 + 相关 MES 数据 + 综合回答

---

## 6. 设备模拟器设计

### 6.1 半真实模拟策略

设备模拟器（Worker Service）每 10 秒执行一次，逻辑如下:

```
1. 查询所有在产工单（状态 = InProgress）
2. 获取工单关联的设备
3. 根据工单状态推算设备应有状态:
   - 工单在跑 → 设备 Running（80%）或 Idle（20%）
   - 工单暂停 → 设备 Idle 或 Maintenance
   - 无工单 → 设备 Idle 或 Offline
4. 注入随机异常事件:
   - 5% 概率触发报警（Alarm）→ 持续 2-10 分钟后恢复
   - 2% 概率触发停机（Maintenance）→ 持续 20-60 分钟
5. 根据设备状态生成产量数据:
   - Running: 每分钟产量 = 设备额定产能 ± 10%
   - Idle/Alarm: 产量 = 0
6. 写入 EquipmentStatus、ProductionReport 表
7. 通过 SignalR 推送状态变化到前端
```

### 6.2 预设场景

通过配置文件或管理界面切换场景:

- **场景 A（正常日）**: 90% 时间 Running，OEE > 80%，报警少
- **场景 B（频繁报警）**: 每小时 2-3 次报警，OEE 60-70%
- **场景 C（设备故障）**: 长时间停机，OEE < 50%

### 6.3 数据生成示例

```csharp
public class EquipmentSimulatorWorker : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workOrders = await GetInProgressWorkOrders();
            
            foreach (var workOrder in workOrders)
            {
                var equipment = await GetEquipmentByWorkOrder(workOrder.Id);
                var newState = DetermineEquipmentState(equipment, workOrder);
                
                // 写入状态记录
                await SaveEquipmentStatus(equipment.Id, newState);
                
                // 生成产量数据
                if (newState == EquipmentState.Running)
                {
                    var quantity = CalculateOutput(equipment);
                    await SaveProductionReport(workOrder.Id, quantity);
                }
                
                // SignalR 推送
                await _hubContext.Clients.All.SendAsync(
                    "EquipmentStatusChanged", 
                    new { equipmentId = equipment.Id, state = newState }
                );
            }
            
            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }
}
```

---

## 7. 知识库 RAG 流程


### 7.1 文档上传与向量化流程

```
1. 用户上传文档（PDF/Word/Excel）
   ↓
2. 文档解析器提取文本
   - PDF: iTextSharp
   - Word: DocumentFormat.OpenXml
   - Excel: EPPlus 或 DocumentFormat.OpenXml
   ↓
3. 文本切片（Chunking）
   - 按段落或固定 token 数切分（500-1000 tokens）
   - 保留文档元数据：文件名、章节、页码
   ↓
4. 调用 Embedding API
   - OpenAI text-embedding-ada-002
   - 或 Azure OpenAI
   - 或本地模型（如 all-MiniLM-L6-v2）
   - 生成向量（1536 维）
   ↓
5. 存储到向量数据库
   - pgvector: 直接存在 PostgreSQL
   - 或 Qdrant: 独立向量库
   ↓
6. 保存记录到 Document 和 DocumentChunk 表
```

### 7.2 RAG 查询流程

```
1. 用户提问："A102 报警怎么处理？"
   ↓
2. 问题向量化（使用相同的 Embedding 模型）
   ↓
3. 向量相似度搜索（余弦相似度，Top 5）
   ↓
4. 获取相关文档片段
   ↓
5. 可选：从 MES 数据库查询设备信息
   - Equipment 表查询 A102 的状态、型号、最近报警
   ↓
6. 构造 Prompt：
   """
   上下文：
   {文档片段 1}
   {文档片段 2}
   ...
   
   设备信息：
   设备编号: A102
   设备型号: 冲压机 XYZ-2000
   当前状态: Alarm
   最近报警: E001 - 传感器故障
   
   问题：A102 报警怎么处理？
   
   请基于上述上下文和设备信息回答问题。
   """
   ↓
7. LLM 生成答案
   ↓
8. 返回结构化结果：
   {
     "answer": "根据维修手册，A102 报警 E001 表示传感器故障...",
     "sources": [
       {"docName": "设备维修手册", "page": 23, "similarity": 0.89}
     ],
     "relatedEquipment": {
       "id": "A102",
       "currentStatus": "Alarm",
       "alarmCode": "E001"
     }
   }
```

### 7.3 支持的文档类型

- **SOP（标准作业程序）**: 操作指导
- **维修手册**: 设备故障处理
- **工艺文件**: 工艺参数、质量标准
- **异常处理规范**: 常见问题解决方案

---

## 8. API 设计

### 8.1 MES REST API

```
# 工单管理
GET    /api/workorders                  # 查询工单列表
GET    /api/workorders/{id}             # 查询工单详情
POST   /api/workorders                  # 创建工单
POST   /api/workorders/{id}/start       # 开工
POST   /api/workorders/{id}/pause       # 暂停
POST   /api/workorders/{id}/report      # 报工
POST   /api/workorders/{id}/complete    # 完工

# 设备管理
GET    /api/equipment                   # 查询设备列表
GET    /api/equipment/{id}              # 查询设备详情
GET    /api/equipment/{id}/status       # 查询设备状态历史
GET    /api/equipment/{id}/alarms       # 查询设备报警记录

# 质量管理
GET    /api/quality/inspections         # 查询质检记录
POST   /api/quality/inspections         # 创建质检记录
GET    /api/quality/defects             # 查询不良记录

# 知识库管理
POST   /api/documents/upload            # 上传知识库文档
GET    /api/documents                   # 查询文档列表
DELETE /api/documents/{id}              # 删除文档
```

### 8.2 Agent 对话 API

```
POST   /api/agent/chat
Request:
{
  "conversationId": "uuid",              # 会话 ID（首次为空，后续传入）
  "message": "今天有哪些工单延期？",
  "agentType": "production",             # 指定 Agent 类型（可选）
  "debugMode": true                      # 开启调试模式
}

Response:
{
  "conversationId": "uuid",
  "reply": "今天共有 3 个工单延期...",
  "data": {                              # 结构化数据
    "workOrders": [...]
  },
  "debug": {                             # 调试信息（仅 debugMode=true）
    "sqlExecuted": "SELECT * FROM...",
    "executionTime": "45ms",
    "toolsCalled": ["GetTodayWorkOrders", "AnalyzeDelayedOrders"]
  },
  "suggestedActions": [                  # 建议后续操作
    { "label": "查看详情", "action": "getDetails", "params": {...} }
  ]
}

GET    /api/agent/conversations/{id}    # 获取对话历史
DELETE /api/agent/conversations/{id}    # 清除对话
```

### 8.3 SignalR Hub

```
Hub: /hubs/equipment

客户端订阅事件：
- SubscribeToEquipment(equipmentId)         # 订阅单个设备
- SubscribeToProductionLine(lineId)         # 订阅产线所有设备

服务端推送事件：
- EquipmentStatusChanged                    # 设备状态变化
  Payload: { equipmentId, state, timestamp }
  
- AlarmTriggered                            # 报警触发
  Payload: { equipmentId, alarmCode, message, timestamp }
  
- ProductionReported                        # 报工记录
  Payload: { workOrderId, quantity, timestamp }
  
- WorkOrderCompleted                        # 工单完工
  Payload: { workOrderId, completedQuantity, timestamp }
```

---

## 9. 前端设计

### 9.1 页面结构

```
/                           # 首页 - 仪表盘
  ├── 实时设备状态卡片
  ├── 今日工单概览
  └── OEE 趋势图

/workorders                 # 工单管理
  ├── 工单列表（筛选、排序）
  └── 工单详情（甘特图、报工记录）

/equipment                  # 设备监控
  ├── 设备列表（实时状态）
  └── OEE 分析看板

/quality                    # 质量管理
  ├── 质检记录
  └── 批次追溯

/knowledge                  # 知识库
  ├── 文档管理（上传、分类）
  └── 文档搜索

/agent                      # AI 助手（核心页面）
  ├── 对话界面（类似 ChatGPT）
  ├── Agent 类型切换（生产/质量/OEE/知识库）
  ├── 结构化数据展示（表格、图表）
  └── 调试面板（开发模式）
```

### 9.2 Agent 对话页面设计

```
┌───────────────────────────────────────────┐
│ [生产] [质量] [OEE] [知识库]  [🔧调试]      │ ← Agent 类型切换
├───────────────────────────────────────────┤
│                                           │
│  用户: 今天有哪些工单延期？                 │
│                                           │
│  Agent: 今天共有 3 个工单延期：            │
│  ┌───────────────────────────────────┐   │
│  │ 工单号  产品    延期原因    操作    │   │ ← 结构化数据表格
│  │ WO001  产品A   物料短缺   [详情]   │   │
│  │ WO002  产品B   设备故障   [详情]   │   │
│  │ WO003  产品C   人员不足   [详情]   │   │
│  └───────────────────────────────────┘   │
│  [查看详情] [生成报告]                     │ ← 建议操作
│                                           │
│  [调试信息 ▼]                              │ ← 可折叠调试面板
│  执行工具: GetTodayWorkOrders             │
│  SQL: SELECT * FROM WorkOrders WHERE...   │
│  执行时间: 45ms                            │
│                                           │
├───────────────────────────────────────────┤
│ [输入消息...]                    [发送]    │
└───────────────────────────────────────────┘
```

### 9.3 技术选型细节

- **API 客户端**: Axios + TanStack Query（缓存、重试、乐观更新）
- **实时通信**: @microsoft/signalr
- **图表**: Recharts 或 Apache ECharts
- **表格**: TanStack Table（虚拟滚动、排序、筛选）
- **表单**: React Hook Form + Zod 验证
- **样式**: Tailwind CSS + shadcn/ui 组件
- **状态管理**: TanStack Query（服务端状态）+ Zustand（客户端状态）

---

## 10. 验收标准（第一期 MVP）

### 10.1 功能验收

#### 生产运营 Agent
- ✅ 能回答"今天有哪些工单？"并返回工单列表（JSON + 文字）
- ✅ 能分析"哪些工单延期？"并给出延期原因统计
- ✅ 能生成指定日期的生产日报（产量、完工率、异常汇总）
- ✅ 调试模式显示执行的 SQL 和耗时

#### 质量追溯 Agent
- ✅ 能根据批次号追溯完整链路（工单→工序→设备→人员→物料→质检）
- ✅ 能分析指定产品的不良模式（按工序、按类型统计）
- ✅ 返回数据包含可点击的实体 ID（链接到详情页）

#### OEE 分析 Agent
- ✅ 能计算指定设备/产线的 OEE（可用率、性能率、质量率）
- ✅ 能分析"为什么今天 OEE 低？"并列出停机、报警、质量问题
- ✅ 能查询设备实时状态和最近报警

#### 知识库 Agent
- ✅ 能回答"A102 报警怎么处理？"并引用相关文档章节
- ✅ 支持上传 PDF、Word、Excel 文档并自动向量化
- ✅ 返回答案时显示文档来源（文件名、页码、相似度）
- ✅ 能结合 MES 数据回答（如"这个不良现象可能和哪个工序有关？"）


### 10.2 技术验收

#### 数据模型
- ✅ 所有核心实体已创建（产品、BOM、工单、工序、设备、质检、文档等）
- ✅ 数据库迁移脚本可正常执行
- ✅ 包含测试数据种子（至少 3 个产品、10 个工单、5 台设备、20 条报工记录）
- ✅ 实体关系正确（外键、导航属性）

#### 设备模拟器
- ✅ Worker Service 能根据工单状态生成合理的设备状态
- ✅ 能注入随机报警和停机事件（符合预设概率）
- ✅ 通过 SignalR 实时推送状态变化到前端（延迟 < 1 秒）
- ✅ 支持场景切换（正常日/频繁报警/设备故障）

#### BotSharp 集成
- ✅ 4 个 Agent 插件已注册并可被调用
- ✅ 对话历史能正确保存和恢复
- ✅ 多轮对话能保持上下文（如"再详细说说第一个工单"能正确理解指代）
- ✅ Agent 工具调用失败时有友好错误提示

#### 知识库 RAG
- ✅ 支持 PDF、Word、Excel 文档上传
- ✅ 文档能正确解析和切片（chunk size 500-1000 tokens）
- ✅ 向量搜索返回相关文档片段（Top 5，相似度 > 0.7）
- ✅ RAG 答案引用文档来源

#### 前端
- ✅ Agent 对话页面支持发送消息、展示结构化数据、切换 Agent 类型
- ✅ 调试模式能显示执行过程、SQL、工具调用链
- ✅ 设备监控页面能实时显示状态变化（WebSocket 连接稳定）
- ✅ 响应式设计，支持移动端访问（最小宽度 375px）
- ✅ 结构化数据以表格或卡片形式展示，支持点击跳转详情

#### 性能
- ✅ Agent 响应时间 < 3 秒（不含 LLM 调用时间）
- ✅ 数据库查询优化（复杂查询 < 500ms）
- ✅ SignalR 消息推送延迟 < 1 秒
- ✅ 前端首次加载时间 < 5 秒

### 10.3 测试覆盖

#### 单元测试（覆盖率 > 70%）
- ✅ WorkOrderService 状态流转逻辑
- ✅ OEE 计算公式验证
- ✅ 设备模拟器状态推算逻辑
- ✅ 文档切片算法
- ✅ Agent 工具参数验证

#### 集成测试（使用 Testcontainers）
- ✅ 启动 PostgreSQL 容器并执行迁移
- ✅ 测试复杂查询（追溯链路、OEE 计算）
- ✅ 测试并发写入（多个设备同时报工）
- ✅ 测试完整的 Agent 对话流程（从问题到答案）
- ✅ 验证 SignalR 消息推送
- ✅ 测试文档上传和 RAG 查询

#### Agent 输出验证测试
```csharp
// 测试数据集（问答对）
var testCases = new[]
{
    new {
        Question = "今天有哪些工单延期？",
        ExpectedDataFields = new[] { "workOrders", "totalCount" },
        ExpectedEntityCount = 3,  // 测试数据中预设 3 个延期工单
        MustIncludeKeyword = "延期"
    },
    new {
        Question = "批次 B20260708001 的追溯信息",
        ExpectedDataFields = new[] { "workOrder", "processSteps", "equipment" },
        MustIncludeKeyword = "追溯"
    },
    new {
        Question = "A102 设备今天的 OEE 是多少？",
        ExpectedDataFields = new[] { "oee", "availability", "performance", "quality" },
        MustIncludeKeyword = "OEE"
    }
};

// 验证逻辑
foreach (var testCase in testCases)
{
    var result = await AgentService.Chat(testCase.Question);
    
    // 验证结构化数据
    Assert.NotNull(result.Data);
    foreach (var field in testCase.ExpectedDataFields)
    {
        Assert.True(result.Data.ContainsKey(field), 
            $"Missing expected field: {field}");
    }
    
    // 验证自然语言回答
    Assert.Contains(testCase.MustIncludeKeyword, result.Explanation);
}
```

#### 手工测试清单
```
[ ] 对话流程：能正确理解问题并调用对应的 Agent 工具
[ ] 多轮对话：能记住上下文（"第一个工单"指代明确）
[ ] 错误处理：输入无效问题时给出友好提示（如"请问具体是哪个设备？"）
[ ] 实时推送：设备状态变化时前端立即更新，无需刷新
[ ] 文档上传：PDF/Word/Excel 都能正确解析和检索
[ ] 调试模式：能看到完整的执行过程和中间结果
[ ] 性能：Agent 响应时间 < 3 秒（不含 LLM 调用时间）
[ ] 跨浏览器：Chrome、Edge、Firefox 测试通过
[ ] 移动端：在手机浏览器上能正常使用对话功能
```

---

## 11. 技术风险与缓解措施

### 11.1 已识别风险

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| BotSharp 文档不完善，集成困难 | 高 | 中 | 提前研究 BotSharp 源码和示例项目（PizzaBot），准备降级方案（使用 Semantic Kernel） |
| Agent 产生幻觉，回答不准确 | 高 | 高 | 1) 返回结构化数据供验证 2) 开发模式显示执行过程 3) 第二期加入事实验证机制 |
| pgvector 性能不足（大量文档） | 中 | 低 | 第一期文档量小（< 100 个文档），可接受；第二期可迁移到 Qdrant |
| 设备模拟数据不够真实 | 低 | 中 | 咨询实际 MES 用户，调整模拟逻辑；第三期接入真实 PLC |
| 前后端技术栈分离增加维护成本 | 中 | 低 | 制定清晰的 API 契约，使用 TypeScript 类型生成工具 |
| LLM API 调用延迟影响用户体验 | 中 | 中 | 1) 前端显示加载状态 2) 实现流式响应 3) 缓存常见问题答案 |

### 11.2 技术依赖

| 依赖项 | 版本 | 风险 | 备选方案 |
|--------|------|------|----------|
| BotSharp | latest | 社区项目，更新频繁 | Semantic Kernel、Langchain.NET |
| PostgreSQL + pgvector | 16 + 0.5.x | pgvector 相对较新 | Qdrant、Milvus |
| OpenAI API | text-embedding-ada-002 | 需要 API Key | Azure OpenAI、本地模型（all-MiniLM-L6-v2） |
| Next.js | 14 | 生态成熟，风险低 | - |

---

## 12. 部署架构（第一期）

### 12.1 开发环境

```
开发机
├── MesCopilot.Api (ASP.NET Core)         # http://localhost:5000
├── MesCopilot.DeviceSimulator (Worker)   # 后台运行
├── PostgreSQL (Docker)                   # localhost:5432
└── Next.js Dev Server                    # http://localhost:3000
```

### 12.2 测试环境

```
单台服务器 / 虚拟机
├── MesCopilot.Api (IIS 或 Kestrel)
├── MesCopilot.DeviceSimulator (Windows Service)
├── PostgreSQL (Docker 或独立安装)
├── Next.js (Static Export 或 Node.js)
└── Nginx (反向代理)
```

### 12.3 生产环境（第三期规划）

```
├── Load Balancer
│   ├── MesCopilot.Api (多实例)
│   └── MesCopilot.DeviceSimulator (单实例)
├── PostgreSQL (主从复制)
├── Redis (缓存)
├── Qdrant (向量库)
└── CDN (静态资源)
```

---

## 13. 开发排期估算（第一期）

| 阶段 | 任务 | 预估工作量 |
|------|------|-----------|
| **Phase 1: 基础搭建** | 项目结构、数据库设计、EF Core 迁移 | 3-5 天 |
| **Phase 2: MES 核心** | 工单/设备/质检 CRUD API | 5-7 天 |
| **Phase 3: 设备模拟器** | Worker Service + SignalR | 3-4 天 |
| **Phase 4: Agent 集成** | BotSharp 集成 + 4 个 Agent 插件 | 7-10 天 |
| **Phase 5: 知识库 RAG** | 文档上传、解析、向量化、RAG 查询 | 5-7 天 |
| **Phase 6: 前端开发** | Next.js 页面 + Agent 对话界面 | 7-10 天 |
| **Phase 7: 测试与优化** | 单元测试、集成测试、性能优化 | 5-7 天 |
| **Phase 8: 部署与文档** | 部署脚本、用户文档、演示视频 | 2-3 天 |
| **总计** | | **37-53 天** |

**说明**: 以上估算基于 1 名全栈开发者，每天 6-8 小时有效工作时间。如果是学习项目或业余时间开发，可乘以 1.5-2 倍。

---

## 14. 后续迭代方向（第二期、第三期）

### 第二期（增强与完善）
- 用户认证与授权（JWT + Role-Based Access Control）
- Agent 提示词优化（Few-Shot Learning、Chain-of-Thought）
- 更多 Agent 工具（预测性维护、排程优化、根因分析）
- 报表导出（Excel、PDF）
- Agent 对话历史检索
- 知识库文档版本管理

### 第三期（企业级能力）
- 多租户支持（Tenant Isolation）
- 完整 BOM 展开和工艺路线排程
- 真实 PLC 连接（OPC UA、Modbus TCP）
- Agent 事实验证机制（对比数据库真值）
- 审计日志与合规性报告
- 性能优化（Redis 缓存、分库分表）
- 移动端 App（React Native）
- 国际化支持（i18n）

---

## 15. 参考资源

### 开源项目
- [yuwang/MES](https://github.com/yuwang/MES) - MES 数据模型和业务流程参考
- [BotSharp](https://github.com/scisharp/botsharp) - Multi-Agent Framework
- [ERPNext Manufacturing](https://erpnext.com/docs/user/manual/en/manufacturing) - 业务流程参考
- [Odoo Manufacturing](https://www.odoo.com/documentation/19.0/applications/inventory_and_mrp/manufacturing.html) - 车间执行参考

### 技术文档
- [ASP.NET Core Documentation](https://learn.microsoft.com/en-us/aspnet/core/)
- [Entity Framework Core Documentation](https://learn.microsoft.com/en-us/ef/core/)
- [SignalR Documentation](https://learn.microsoft.com/en-us/aspnet/core/signalr/)
- [pgvector Documentation](https://github.com/pgvector/pgvector)

### 标准与规范
- [ISA-95](https://www.isa.org/standards-and-publications/isa-standards/isa-95-standard) - MES/MOM 业务边界
- [MESA Model](https://mesa.org/topics-resources/mesa-model/) - MES 核心功能定义

---

## 16. 附录

### 16.1 术语表

| 术语 | 英文 | 说明 |
|------|------|------|
| MES | Manufacturing Execution System | 制造执行系统 |
| OEE | Overall Equipment Effectiveness | 设备综合效率 |
| SOP | Standard Operating Procedure | 标准作业程序 |
| BOM | Bill of Materials | 物料清单 |
| RAG | Retrieval-Augmented Generation | 检索增强生成 |
| Agent | - | 智能代理，能理解自然语言并调用工具执行任务 |

### 16.2 常见问题

**Q: 为什么选择 BotSharp 而不是 Semantic Kernel？**  
A: BotSharp 是完整的 Multi-Agent 框架，自带对话管理、插件系统、UI；Semantic Kernel 更偏底层库。如果 BotSharp 集成困难，可降级到 Semantic Kernel。

**Q: 为什么选择 React 而不是 Blazor？**  
A: React 生态更成熟，UI 组件库更丰富（shadcn/ui、Ant Design），前后端分离便于独立部署。Blazor 适合全栈 C# 团队。

**Q: 第一期为什么不做用户认证？**  
A: 为了加速 MVP 验证，专注于 Agent 功能的可行性。第二期再加入认证和权限控制。

**Q: Agent 的幻觉问题怎么解决？**  
A: 第一期通过返回结构化数据供验证，第二期加入事实验证机制（对比数据库真值），第三期引入人工审核流程。

---

**文档版本**: v1.0  
**最后更新**: 2026-07-08  
**作者**: AI Agent  
**状态**: 待评审

