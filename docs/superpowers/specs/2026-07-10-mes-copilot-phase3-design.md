# MES Copilot Phase 3 Design Spec — 企业级能力

**项目名称**: MES Copilot Phase 3
**创建日期**: 2026-07-10
**设计版本**: v1.0
**前置条件**: Phase 1 (MVP) + Phase 2 (认证/Prompt/流式/报表) 已完成

---

## 1. 目标

将 MES Copilot 从可演示的 MVP 升级为**生产可用的企业级 SaaS 平台**，具备：

1. **多租户隔离** — 单实例服务多个工厂/企业，数据完全隔离
2. **完整 BOM 展开 + 工艺路线排程** — 多层物料需求计算 + 按工序顺序自动排程
3. **真实 PLC 连接** — OPC UA / Modbus TCP / MQTT 三协议支持（含模拟器验证）
4. **Agent 事实验证增强** — SQL 回查 + 逻辑一致性检查，减少幻觉
5. **全链路审计** — API 操作日志 + 数据变更追踪 + Agent 决策审计
6. **性能优化** — Redis 缓存层 + PostgreSQL 读写分离
7. **国际化 (i18n)** — 中英双语，前后端完整多语言支持

## 2. 非目标（明确排除）

- ❌ 移动端 App（React Native）— 推迟到第四期
- ❌ 分库分表 — 当前数据量级不需要
- ❌ 完整 MRP 计算 — MES 不做 ERP 的活，只做工序级排程
- ❌ 排程优化算法（遗传算法/约束规划）— 标记为 future work
- ❌ Agent 人工审核流 — 事实验证做到 L2，审核流推后
- ❌ 真实 PLC 设备对接 — 用模拟器验证架构，接口设计生产级
- ❌ 多区域部署 / CDN — 第四期再考虑

## 3. 架构决策

| 决策项 | 选型 | 理由 |
|--------|------|------|
| 多租户策略 | 共享数据库 + TenantId 列过滤 | Finbuckle.MultiTenant 直接支持，中小 SaaS 够用 |
| 多租户库 | Finbuckle.MultiTenant (1.2k star) | .NET 生态最成熟的多租户库 |
| 缓存 | Redis (StackExchangeRedis) | 微软官方推荐，分布式缓存标准 |
| 读写分离 | PostgreSQL Streaming Replication + Npgsql MultiHost | 零代码改动，连接字符串配置主从 |
| OPC UA | OPCFoundation/UA-.NETStandard (MIT) | OPC 基金会官方实现 |
| Modbus TCP | NModbus4 (600+ star) | 轻量稳定，API 简洁 |
| MQTT | MQTTnet (4k+ star) | 微软背书，支持 Client + Server |
| 审计日志 | Audit.NET (2.2k star) | EF Core 拦截 + 自动变更记录 |
| 前端 i18n | next-intl | Next.js App Router 官方推荐 |
| 后端 i18n | IStringLocalizer + .resx | ASP.NET Core 内置，零额外依赖 |
| BOM 展开 | 自研递归算法 | 逻辑简单，无需第三方库 |
| 排程 | 顺序排程（工序顺序 + 设备可用性） | 满足 MES 级需求，不过度设计 |


## 4. Sub-Phase 分组与依赖关系

```
3A: 平台基础 ──────────────────────────┐
  (多租户 + 审计 + Redis + 读写分离)     │
                                         ▼
3B: 制造深化 ──────── 依赖 3A 的多租户过滤
  (BOM展开/排程 + PLC连接)               │
                                         ▼
3C: AI增强 + i18n ─── 依赖 3A 的缓存层
  (事实验证L2 + 国际化)
```

### Sub-Phase 3A: 平台基础（预估 10-14 天）

**模块清单:**
1. 多租户支持 (Finbuckle.MultiTenant)
2. 全链路审计日志 (Audit.NET + Agent 审计)
3. Redis 缓存层
4. PostgreSQL 读写分离

**关键交付物:**
- 所有实体表增加 TenantId 列 + Global Query Filter
- 租户管理 API（CRUD + 切换）
- 审计日志表 + 查询 API + 合规报告导出
- Agent 决策日志记录（工具调用链、输入输出、验证结果）
- Redis 缓存：设备状态(TTL 10s)、Agent 回答(TTL 5min)、Token 黑名单
- 主从配置 + 读写分离连接字符串

### Sub-Phase 3B: 制造深化（预估 12-16 天）

**模块清单:**
1. 多层 BOM 展开算法
2. 工序顺序排程引擎
3. PLC 连接层（OPC UA + Modbus TCP + MQTT）
4. PLC 模拟器（扩展 DeviceSimulator）

**关键交付物:**
- BOM 递归展开 API（输入产品+数量，输出各层物料需求）
- 排程 API（输入工单，输出工序时间表 + 设备分配）
- 排程甘特图数据接口
- IDeviceConnector 统一抽象 + 3 个协议实现
- 设备连接管理 API（添加/删除/测试连接）
- OPC UA Server 模拟器 + Modbus Slave 模拟器 + MQTT Publisher 模拟器
- 前端设备连接配置页面

### Sub-Phase 3C: AI 增强 + i18n（预估 8-10 天）

**模块清单:**
1. Agent 事实验证 L2（SQL 回查 + 逻辑一致性）
2. 前端国际化 (next-intl)
3. 后端国际化 (IStringLocalizer)

**关键交付物:**
- 验证 SQL 自动生成器（根据 Agent 回复中的数值声明生成验证查询）
- 逻辑一致性检查器（列表数量 vs 声明数量、百分比加总 vs 100%）
- 验证结果标注在 Agent 回复中（✓ 已验证 / ⚠ 存疑）
- 前端所有页面文字提取为 i18n key
- 中英文语言包（zh-CN.json / en-US.json）
- 语言切换 UI 组件
- 后端 API 错误信息 + Agent 提示词模板多语言化

## 5. 数据模型变更

### 5.1 多租户扩展

所有现有实体表增加：
```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}
```

新增实体：
```csharp
// 租户主数据
public class Tenant
{
    public string Id { get; set; }          // GUID
    public string Name { get; set; }        // 企业名称
    public string Code { get; set; }        // 租户编码
    public string? ConnectionString { get; set; } // 预留独立库
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 5.2 审计日志

```csharp
// API 操作日志
public class AuditLog
{
    public long Id { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string UserName { get; set; }
    public string Action { get; set; }       // GET/POST/PUT/DELETE
    public string Endpoint { get; set; }     // /api/workorders/1
    public string? RequestBody { get; set; }
    public int ResponseCode { get; set; }
    public long DurationMs { get; set; }
    public DateTime Timestamp { get; set; }
    public string? IpAddress { get; set; }
}

// 数据变更日志（Audit.NET 自动生成）
public class DataChangeLog
{
    public long Id { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string EntityType { get; set; }   // WorkOrder
    public string EntityId { get; set; }     // 1
    public string ChangeType { get; set; }   // Insert/Update/Delete
    public string? OldValues { get; set; }   // JSON
    public string? NewValues { get; set; }   // JSON
    public DateTime Timestamp { get; set; }
}

// Agent 决策日志
public class AgentAuditLog
{
    public long Id { get; set; }
    public string TenantId { get; set; }
    public string UserId { get; set; }
    public string ConversationId { get; set; }
    public string AgentType { get; set; }    // production/quality/oee/knowledge
    public string UserQuestion { get; set; }
    public string ToolsCalled { get; set; }  // JSON array
    public string? SqlExecuted { get; set; } // JSON array
    public string AgentReply { get; set; }
    public bool IsVerified { get; set; }
    public string? VerificationDetails { get; set; } // JSON
    public long ResponseTimeMs { get; set; }
    public DateTime Timestamp { get; set; }
}
```

### 5.3 BOM 展开结果

```csharp
// BOM 展开结果（计算后的物料需求）
public class BomExplosionResult
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int MaterialId { get; set; }
    public int BomLevel { get; set; }        // BOM 层级（1=直接件）
    public decimal RequiredQuantity { get; set; }
    public decimal AvailableQuantity { get; set; }
    public decimal ShortageQuantity { get; set; }
    public DateTime CalculatedAt { get; set; }
}

// 排程结果
public class ScheduleEntry
{
    public int Id { get; set; }
    public int WorkOrderId { get; set; }
    public int ProcessStepId { get; set; }
    public int EquipmentId { get; set; }
    public DateTime PlannedStart { get; set; }
    public DateTime PlannedEnd { get; set; }
    public int Sequence { get; set; }
    public string Status { get; set; }       // Planned/InProgress/Completed
    public DateTime CreatedAt { get; set; }
}
```

### 5.4 设备连接配置

```csharp
// 设备连接配置
public class DeviceConnection
{
    public int Id { get; set; }
    public string TenantId { get; set; }
    public int EquipmentId { get; set; }
    public string Protocol { get; set; }     // OpcUa/ModbusTcp/Mqtt
    public string ConnectionString { get; set; } // 协议特定连接参数 JSON
    public bool IsActive { get; set; }
    public DateTime? LastConnectedAt { get; set; }
    public string? LastError { get; set; }
    public DateTime CreatedAt { get; set; }
}
```


## 6. API 设计变更

### 6.1 多租户 API

```
# 租户管理（仅 SuperAdmin）
GET    /api/tenants                    # 查询租户列表
POST   /api/tenants                    # 创建租户
PUT    /api/tenants/{id}               # 更新租户
DELETE /api/tenants/{id}               # 停用租户

# 租户切换
POST   /api/tenants/switch/{tenantId}  # 切换当前租户上下文
GET    /api/tenants/current            # 获取当前租户信息
```

### 6.2 审计日志 API

```
# 审计查询
GET    /api/audit/operations           # 操作日志查询（分页+筛选）
GET    /api/audit/changes              # 数据变更日志
GET    /api/audit/agent                # Agent 决策日志
GET    /api/audit/report               # 合规报告导出（PDF/Excel）

# 筛选参数
?userId=xxx&startDate=2026-07-01&endDate=2026-07-10&entityType=WorkOrder&action=POST
```

### 6.3 BOM 展开 + 排程 API

```
# BOM 展开
POST   /api/bom/explode                # 计算物料需求
  Body: { productId: 1, quantity: 1000 }
  Response: { levels: [...], totalMaterials: [...], shortages: [...] }

GET    /api/bom/{productId}/tree       # 获取 BOM 树形结构

# 排程
POST   /api/scheduling/generate        # 生成排程计划
  Body: { workOrderIds: [1,2,3], strategy: "sequential" }
  Response: { entries: [...], ganttData: [...] }

GET    /api/scheduling/workorder/{id}  # 获取工单排程
PUT    /api/scheduling/entries/{id}    # 手动调整排程条目
GET    /api/scheduling/gantt           # 甘特图数据（前端渲染）
```

### 6.4 设备连接 API

```
# 连接管理
GET    /api/device-connections                  # 查询所有连接
POST   /api/device-connections                  # 添加连接配置
PUT    /api/device-connections/{id}             # 更新连接
DELETE /api/device-connections/{id}             # 删除连接
POST   /api/device-connections/{id}/test        # 测试连通性
POST   /api/device-connections/{id}/start       # 启动数据采集
POST   /api/device-connections/{id}/stop        # 停止数据采集

# 实时数据
GET    /api/device-connections/{id}/realtime    # 获取实时采集数据
```

### 6.5 事实验证 API

```
# Agent 回复验证（内部调用，也可手动触发）
POST   /api/agent/verify
  Body: { conversationId: "uuid", messageId: "uuid" }
  Response: { isVerified: true, checks: [...], discrepancies: [...] }
```

### 6.6 国际化

所有 API 响应根据 `Accept-Language` header 返回对应语言的错误信息和提示。

```
Request Header: Accept-Language: en-US
Response: { "error": "Work order not found" }

Request Header: Accept-Language: zh-CN
Response: { "error": "工单未找到" }
```

## 7. 前端设计变更

### 7.1 新增页面

```
/admin/tenants              # 租户管理（SuperAdmin）
/admin/audit                # 审计日志查看
/admin/audit/report         # 合规报告生成
/admin/device-connections   # 设备连接配置
/scheduling                 # 排程管理（甘特图）
/scheduling/bom-explode     # BOM 展开结果
/settings/language          # 语言设置
```

### 7.2 前端风格（延续 Phase 1）

沿用现有设计系统：
- **框架**: Next.js 14 App Router
- **组件库**: shadcn/ui
- **样式**: Tailwind CSS
- **图表**: Recharts
- **表格**: TanStack Table
- **主色调**: teal-600（延续登录页设计）
- **新增**: 甘特图组件（使用 @nivo/bar 或 react-gantt-timeline）
- **新增**: 语言切换下拉（Header 右侧）

### 7.3 排程甘特图设计

```
┌─────────────────────────────────────────────────────────┐
│ 排程管理                          [生成排程] [导出]      │
├─────────────────────────────────────────────────────────┤
│ 设备 ▼  │ 07:00  08:00  09:00  10:00  11:00  12:00    │
│─────────┼──────────────────────────────────────────────│
│ A101    │ [===WO-001 冲压===]  [==WO-002==]            │
│ A102    │      [====WO-003 冲压====]                   │
│ B201    │ [===WO-001 焊接========]                     │
│ B202    │           [===WO-004 焊接===]                │
│ C301    │                [==WO-001 检测==]             │
├─────────────────────────────────────────────────────────┤
│ 颜色图例: ■ 进行中  ■ 计划  ■ 延期  ■ 完成            │
└─────────────────────────────────────────────────────────┘
```

### 7.4 Agent 事实验证 UI

Agent 回复中显示验证状态：
```
┌────────────────────────────────────────────┐
│ Agent: 今天共有 5 个工单延期...            │
│                                            │
│ ✓ 已验证  [查看验证详情 ▼]                │
│   • 工单数量: 5 ✓ (SQL回查一致)           │
│   • 列表一致性: 5项 ✓                     │
│   • 延期天数: 各项核实通过 ✓              │
└────────────────────────────────────────────┘
```

或存疑时：
```
┌────────────────────────────────────────────┐
│ Agent: 本周 OEE 为 85%...                 │
│                                            │
│ ⚠ 部分存疑  [查看验证详情 ▼]             │
│   • OEE 数值: 85% ✓                       │
│   • 可用率声明: 92% ⚠ 实际 89.5%         │
└────────────────────────────────────────────┘
```

## 8. 技术选型与开源参考

| 模块 | 库/框架 | GitHub Stars | 许可证 | 备注 |
|------|---------|-------------|--------|------|
| 多租户 | Finbuckle.MultiTenant | 1.2k+ | Apache-2.0 | .NET 多租户标准方案 |
| 审计日志 | Audit.NET | 2.2k+ | MIT | EF Core 集成，自动 Change Tracking |
| Redis 缓存 | StackExchange.Redis | 5.8k+ | MIT | 微软官方推荐 |
| OPC UA | OPCFoundation/UA-.NETStandard | 1.8k+ | MIT | OPC 基金会官方 |
| Modbus TCP | NModbus4 | 600+ | MIT | 稳定的 Modbus .NET 实现 |
| MQTT | MQTTnet | 4k+ | MIT | 微软背书，支持 Client + Server |
| 前端 i18n | next-intl | 2k+ | MIT | Next.js App Router 推荐方案 |
| 后端 i18n | Microsoft.Extensions.Localization | 内置 | MIT | ASP.NET Core 官方 |
| 甘特图 | gantt-task-react | 800+ | MIT | 轻量 React 甘特图 |
| 读写分离 | Npgsql MultiHost | 内置 | PostgreSQL | Npgsql 原生支持 Target Session Attributes |

## 9. 验收标准

### 9.1 Sub-Phase 3A 验收

#### 多租户
- [ ] 创建租户后，该租户只能看到自己的数据
- [ ] 切换租户上下文后，所有查询自动过滤
- [ ] 跨租户访问返回 403 Forbidden
- [ ] 现有 API 无感知改动（通过 middleware 注入 TenantId）
- [ ] SuperAdmin 可查看所有租户数据

#### 审计日志
- [ ] 所有 API 调用自动记录操作日志（无需手动埋点）
- [ ] 工单/设备/质检等实体的增删改自动记录变更前后值
- [ ] Agent 每次对话记录：问题、工具调用链、SQL、回复、验证结果
- [ ] 审计日志查询支持按用户/时间/操作类型筛选
- [ ] 合规报告可导出为 PDF/Excel

#### Redis 缓存
- [ ] 设备实时状态从缓存读取（TTL 10s），减少数据库查询 80%+
- [ ] Agent 相同问题 5 分钟内命中缓存，响应 < 100ms
- [ ] Token 黑名单通过 Redis 实现（登出后立即失效）
- [ ] 缓存失效策略正确（数据变更时主动清除）

#### 读写分离
- [ ] 读操作走从库，写操作走主库
- [ ] 主从延迟 < 1 秒
- [ ] 从库故障时自动 fallback 到主库

### 9.2 Sub-Phase 3B 验收

#### BOM 展开
- [ ] 支持 3 层以上 BOM 递归展开
- [ ] 正确计算各层物料需求量（父件用量 × 子件用量）
- [ ] 识别物料短缺并给出缺口数量
- [ ] 循环引用检测（A→B→C→A 报错而非死循环）

#### 排程
- [ ] 根据工艺路线自动生成工序时间表
- [ ] 考虑设备可用性（已被占用的时段跳过）
- [ ] 排程结果可通过甘特图可视化
- [ ] 支持手动拖拽调整排程条目
- [ ] 排程冲突检测（同一设备同一时段不重叠）

#### PLC 连接
- [ ] OPC UA 模拟器可启动并被 OPC UA Client 正常订阅
- [ ] Modbus TCP 模拟器可启动并被 Modbus Client 正常读取寄存器
- [ ] MQTT 模拟器可发布消息并被 MQTT 订阅者接收
- [ ] 统一 IDeviceConnector 接口：Connect/Disconnect/Subscribe/Read/Write
- [ ] 连接配置页面可添加/测试/启停连接
- [ ] 采集数据写入 EquipmentStatus 表并通过 SignalR 推送

### 9.3 Sub-Phase 3C 验收

#### 事实验证 L2
- [ ] Agent 回复中的数值声明自动生成验证 SQL
- [ ] SQL 回查结果与 Agent 声明对比，不一致时标记 ⚠
- [ ] 逻辑一致性：列表项数 == 声明总数
- [ ] 逻辑一致性：百分比明细加总 ≈ 总百分比（±1% 容差）
- [ ] 前端显示验证状态（✓ 已验证 / ⚠ 存疑）

#### 国际化
- [ ] 前端支持中英文切换，切换后所有界面文字即时更新
- [ ] 后端 API 错误信息根据 Accept-Language 返回对应语言
- [ ] Agent 提示词模板按用户语言偏好切换
- [ ] 日期/数字格式跟随 locale（如 2026-07-10 vs 07/10/2026）
- [ ] 新增页面/组件默认使用 i18n key（不出现硬编码文字）

## 10. 测试矩阵

### 10.1 单元测试

| 模块 | 测试内容 | 预估用例数 |
|------|---------|-----------|
| 多租户 | TenantId 注入、Global Filter 生效、跨租户隔离 | 15+ |
| 审计日志 | AuditMiddleware 拦截、ChangeTracking 正确性 | 12+ |
| BOM 展开 | 递归展开、用量计算、循环引用检测、空 BOM | 20+ |
| 排程算法 | 顺序排程、设备冲突、空闲时段查找 | 15+ |
| 设备连接 | IDeviceConnector 接口行为、连接状态管理 | 10+ |
| 事实验证 | SQL 生成、数值提取、逻辑一致性规则 | 18+ |
| i18n | 语言包加载、key 缺失 fallback、locale 格式化 | 8+ |

### 10.2 集成测试

| 场景 | 测试内容 |
|------|---------|
| 多租户隔离 | 创建 2 个租户，各自创建工单，互相看不到对方数据 |
| 审计完整链路 | 创建工单 → 查审计日志 → 验证字段完整性 |
| BOM 端到端 | 创建产品+BOM → 调用展开 API → 验证物料需求 |
| 排程端到端 | 创建工单 → 生成排程 → 验证不冲突 → 获取甘特图数据 |
| PLC 模拟器 | 启动 OPC UA 模拟 → 连接 → 订阅 → 验证数据流 |
| Agent 审计 | Agent 对话 → 查 AgentAuditLog → 验证工具链记录 |
| 缓存命中 | 同一查询连续调用 → 第二次 < 100ms |
| 读写分离 | 写入数据 → 从库查询 → 验证一致性 |

### 10.3 前端测试

| 页面 | 测试内容 |
|------|---------|
| 租户管理 | CRUD 操作、切换租户 |
| 审计日志 | 筛选、分页、导出 |
| 排程甘特图 | 渲染、拖拽调整、冲突提示 |
| 设备连接 | 添加配置、测试连通性、启停 |
| 验证状态 | ✓/⚠ 图标显示、详情展开 |
| 语言切换 | 切换后全页面文字更新、持久化偏好 |

## 11. 影响范围分析

### 11.1 后端变更

| 项目 | 变更类型 | 影响程度 |
|------|---------|---------|
| MesCopilot.Domain | 新增实体 + ITenantEntity 接口 | 中 |
| MesCopilot.Infrastructure | DbContext 改造 + 新增 Repository + 缓存层 | 高 |
| MesCopilot.Application | Service 层适配多租户 + 新增 BOM/排程服务 | 高 |
| MesCopilot.Agent | 验证增强 + i18n 提示词 + 审计 wrapper | 中 |
| MesCopilot.Api | 新增 Controller + Middleware + 读写分离配置 | 高 |
| MesCopilot.DeviceSimulator | 扩展为 PLC 模拟器（3 协议） | 高 |

### 11.2 前端变更

| 目录 | 变更类型 | 影响程度 |
|------|---------|---------|
| web/app/ | 新增 6 个页面路由 | 中 |
| web/components/ | 新增甘特图、验证状态、语言切换组件 | 中 |
| web/lib/ | 所有 API 调用加 tenant header + i18n 集成 | 高（全量改动） |
| web/messages/ | 新增中英文语言包 | 新增 |
| web/middleware.ts | 增加 locale 路由处理 | 低 |

### 11.3 数据库变更

- 所有现有表增加 `TenantId` 列（NOT NULL, 带默认值用于迁移）
- 新增 6 张表：Tenant, AuditLog, DataChangeLog, AgentAuditLog, BomExplosionResult, ScheduleEntry, DeviceConnection
- 新增索引：TenantId 组合索引、审计日志时间索引
- PostgreSQL 主从复制配置

### 11.4 基础设施变更

- 新增 Redis 实例（Docker 或独立部署）
- PostgreSQL 主从配置（可用 Docker Compose 模拟）
- OPC UA / Modbus / MQTT 模拟器进程

## 12. 技术风险与缓解

| 风险 | 影响 | 概率 | 缓解措施 |
|------|------|------|----------|
| 多租户改造涉及所有现有查询 | 高 | 中 | Finbuckle Global Filter 自动化，减少手动改动 |
| BOM 循环引用导致栈溢出 | 高 | 低 | 递归深度限制 + visited set 检测 |
| OPC UA 库学习曲线陡峭 | 中 | 中 | 先用官方 Sample Server 跑通，再自定义 |
| 读写分离主从延迟影响一致性 | 中 | 中 | 写后读走主库（sticky session 策略） |
| i18n 全量文字提取遗漏 | 低 | 高 | CI 脚本扫描硬编码中文 |
| 排程算法性能（大量工单） | 中 | 低 | 限制单次排程 ≤ 50 工单，超出分批 |

## 13. 非目标（明确不做）

- ❌ 完整 MRP/APS 高级排程算法（遗传算法、约束规划）
- ❌ 真实 PLC 硬件对接（本期只做模拟器验证）
- ❌ 移动端 App（推迟到第四期）
- ❌ 多数据库租户隔离（只做共享库 + TenantId）
- ❌ Agent 回复的人工审核工作流（标记存疑即可，不做审批链）
- ❌ 分库分表（读写分离已足够）
- ❌ 第三方 ERP 集成（排程数据暂不对接外部系统）
- ❌ 除中英文外的其他语言

## 14. 开发排期估算

| Sub-Phase | 任务 | 预估工作量 |
|-----------|------|-----------|
| **3A-1: 多租户** | Finbuckle 集成 + 实体改造 + API | 4-5 天 |
| **3A-2: 审计日志** | Audit.NET + AgentAudit + 查询API | 3-4 天 |
| **3A-3: Redis 缓存** | StackExchangeRedis + 缓存策略 | 2-3 天 |
| **3A-4: 读写分离** | Npgsql MultiHost + 配置 | 1-2 天 |
| **3B-1: BOM 展开** | 递归算法 + API + 测试 | 3-4 天 |
| **3B-2: 排程引擎** | 顺序排程 + 甘特图数据 + 前端 | 4-5 天 |
| **3B-3: PLC 连接** | 3 协议实现 + 模拟器 + 管理页面 | 5-7 天 |
| **3C-1: 事实验证 L2** | SQL 生成 + 逻辑检查 + 前端标注 | 3-4 天 |
| **3C-2: 国际化** | next-intl + IStringLocalizer + 语言包 | 3-4 天 |
| **测试与集成** | 集成测试 + E2E + 性能验证 | 4-5 天 |
| **总计** | | **32-43 天** |

基于 1 名全栈开发者，每天 6-8 小时。

---

**文档版本**: v1.0
**创建日期**: 2026-07-10
**作者**: AI Agent
**状态**: 待评审
**前置依赖**: Phase 1 (已完成) + Phase 2 (已完成)
