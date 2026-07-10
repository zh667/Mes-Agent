# Phase 3 完整代码审查报告（A/B/C/D）

**分支**: `codex/phase3-tenancy`
**审查日期**: 2026-07-10
**审查范围**: Phase 3A（租户隔离）、3B（BOM/排程/设备采集）、3C（事实验证）、3D（国际化）
**审查方法**: 并行 9 个 specialized agent，分领域深度审查

---

## 执行摘要

Phase 3 实现了 86 个文件变更（+8977 / -1166 行），通过了全部自动化验证（构建、单元/集成测试、覆盖率 70.42%、格式化、前端检查），但发现 **15 个阻塞级问题**、**20 个高危问题**和 **30 个中危问题**，需要在合并前修复。

### 核心风险

1. **租户隔离不完整**：SignalR Hub 无认证、排程可跨租户分配设备、审计日志租户级隔离限制平台管理员调查
2. **国际化系统未连通**：locale 未传入 Agent prompt，全部用户收到英文响应
3. **基础设施安全**：Redis/MQTT 无认证、MQTT 明文传输、Data Protection 密钥未持久化
4. **部署风险**：迁移未包裹事务、验证脚本允许默认密码通过、备份命令参数缺失

---

## 阻塞级问题（BLOCKING）— 必须修复才能合并

### 租户安全（5 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B1 | `Hubs/EquipmentHub.cs` | 全文件 | **SignalR Hub 无 `[Authorize]`、无租户校验**。任何匿名连接可订阅其他租户设备，实时泄露生产数据。 |
| B2 | `SequentialSchedulingService.cs` | 87-90 | 查已占用工单仅依赖 EF 全局过滤器，若 `CurrentTenantId` 为空则过滤器失效，可跨租户污染排程。 |
| B3 | `Tenant.cs` / `UserTenantMembership.cs` | N/A | **未实现 `ITenantEntity`**，全局过滤器不生效。若任一端点遗漏 `RequirePlatformAdmin`，租户元数据跨租户泄露。 |
| B4 | `AgentVerificationController.cs` | 72 | **事实验证 recheck API 租户隔离绕过**：通过 userId 过滤消息但用当前租户上下文执行验证查询，管理员可跨租户 recheck 探测数据存在性。 |
| B5 | `Program.cs` | 86 | PlatformAdmin policy 大小写不一致（policy 用 `ToLowerInvariant()`，中间件用 `OrdinalIgnoreCase`），可能导致授权失败。 |

### 并发安全（1 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B6 | `SequentialSchedulingService.cs` | 194-201 | **`AdjustOperationAsync` TOCTOU 竞态**：overlap 检查和 SaveChanges 之间无事务/行锁，并发请求可双重预约设备。 |

### 数据库迁移（1 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B7 | `20260710063822_AddTenantIsolation.cs` | 296-677 | **22 张表加 NOT NULL `TenantId` + 唯一约束，操作未包裹显式事务**。若约束添加失败，表处于半成品状态，只能手动 SQL 恢复。 |

### 国际化（4 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B8 | `AgentController.cs` | 104, 278-310 | **locale 未传给 Agent prompt builder**。所有用户始终收到英文 prompt，国际化系统完全失效。 |
| B9 | `AgentController.cs` | 171, 225 | 关键错误消息硬编码英文（"Agent processing failed."、"Search query is required."）。 |
| B10 | `AuthController.cs` | 88-128 | 认证错误全部硬编码英文（"Invalid credentials."、"Invalid token." 等 5 处）。 |
| B11 | `ApiProblemFactory.cs` | 33 | **缺失本地化 key 时抛出的异常消息为硬编码英文**，泄露给生产用户。 |

### 基础设施（2 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B12 | `scripts/verify-phase3-compose.ps1` | 21-25 | **验证脚本只检查 `.env.example` 占位符，不检查实际 `.env` 是否为生产密码**。可能用默认密码部署生产。 |
| B13 | `docs/deployment/phase3-deployment.md` | 12 | **`pg_dump` 缺 host/port/user 参数**。备份失败或备份错误数据库，回滚不可行。 |

### BOM/库存（1 个）

| ID | 文件 | 行 | 问题 |
|----|------|----|----|
| B14 | `BomExplosionService.cs` | 70 | `ToDictionaryAsync(b => b.MaterialId, ...)` 在全局过滤器失效时遇到重复 key 直接抛 `ArgumentException`，500 不可恢复。 |

---

## 高危问题（HIGH）— 强烈建议合并前修复

### 基础设施安全（6 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H1 | Redis | `docker-compose.yml:20-29` | **Redis 无 `requirepass`**。同网络容器可 flush 撤销列表，使已注销 JWT 复活。 |
| H2 | MQTT | `mosquitto.conf` | **`allow_anonymous true`、无 ACL**。任何客户端可发布伪造设备数据。 |
| H3 | MQTT | `MqttDeviceConnector.cs:31-38` | **MQTT 不启用 TLS**，用户名/密码明文传输。 |
| H4 | OPC UA | `OpcUaDeviceConnector.cs:56-65` | `CertificateThumbprint` 为空时降级到 `SecurityMode.None`，中间人攻击入口。 |
| H5 | Data Protection | `DependencyInjection.cs:73-78` | **密钥路径未强制配置**。容器重启后密钥环丢失，所有加密设备密码不可解。 |
| H6 | 健康检查 | `Program.cs:141-144` | `/health` 只检 Redis，不检 PG / MQTT / OPC UA。负载均衡器将流量路由到断连实例。 |

### 数据库与迁移（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H7 | 迁移 | `20260710065225_AddTenantAuthorization.cs:13-15` | **`DropColumn("Role", "AspNetUsers")` 不可逆数据销毁**。若前序迁移失败/顺序错乱，Role 信息永久丢失。 |
| H8 | 租户过滤 | `MesDbContext.cs:115-132` | 租户过滤器无 null 检查。`CurrentTenantId` 为空时过滤器变为 `TenantId == null`，可能泄露未初始化记录。 |

### 设备与后台任务（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H9 | 设备运行 | `DeviceCollectionCoordinator.cs:132-155` | `ProcessReadingAsync` DB 写失败异常冒泡导致整条设备连接断开重连，DB 短暂故障 → 所有设备 thundering herd。 |
| H10 | 审计 | `AuditLogWriter.cs:20-23` | 当 TenantContext 为空时自行注入 `IsPlatformAdmin=true`，可能写入错误分区或绕过写守卫。 |

### 排程与业务逻辑（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H11 | 排程安全 | `SequentialSchedulingService.cs:191-198` | `AdjustOperationAsync` 未验证新 `equipmentId` 的 TenantId，可跨租户分配设备。 |
| H12 | 排程逻辑 | `SequentialSchedulingService.cs:191` | 缺少父 WorkOrder 状态检查，已完成/已取消的工单操作仍可被调整。 |

### 事实验证（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H13 | SQL 风险 | `VerificationQueryService.cs:78` | `Contains(documentIds)` 无集合大小限制，Agent 可生成数千 ID 导致查询性能下降或内存耗尽。 |
| H14 | 认证 | `Program.cs:64-80` | Token 撤销检查在 `OnTokenValidated` 之后，分布式 Redis 延迟下存在毫秒级竞态窗口。 |

### API 契约（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H15 | OpenAPI | `schema.ts:3547-3552` | `BomExplosionRequest.productId/quantity` 在 spec 中非 required，前端可编译通过但后端 400 拒绝。 |
| H16 | OpenAPI | OpenAPI `/api/bom/explode` | 控制器返回 404/409，但 OpenAPI 只声明 200/400/403，前端无对应类型。 |

### 部署（2 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H17 | 验证 | `verify-phase3-compose.ps1:28-44` | 健康检查只测 `/health` 可用性，不验证租户隔离/认证/迁移状态。验证通过但租户隔离已失效。 |
| H18 | 回滚 | `phase3-deployment.md:32` | 回滚文档未说明如何处理迁移后新写入的数据、审计日志保留，可能违反合规要求。 |

### BOM/库存（1 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H19 | BOM | `BomExplosionService.cs:89-103` | 同一物料在多层 BOM 中各行独立显示 `AvailableQuantity`，逐行未扣减已分配量，误导操作员。 |

### 测试基础设施（1 个）

| ID | 领域 | 文件 / 行 | 问题 |
|---|------|-----------|------|
| H20 | 覆盖率 | `ci.yml:35` | **70% 覆盖率门槛未在 CI 强制执行**，覆盖率可静默回退。 |

---

## 中危问题（MEDIUM）— 30 个

详见完整清单：

### 设备协议（5 个）
- M1: OPC UA 不配置 CRL/OCSP
- M2: Modbus `RegisterAddress + RegisterCount` 可溢出 65535
- M3: Modbus TCP 无 Keepalive
- M4: Coordinator `_ = RunAsync(...)` fire-and-forget
- M5: MQTT `persistence false`

### 基础设施（4 个）
- M6: PG replica `pg_hba.conf` 允许 all host 复制连接
- M7: PG 5432 端口暴露宿主 + 默认密码
- M8: 迁移顺序耦合（`AddTenantIsolation` SQL 引用 `Role` 列）
- M9: 事实验证迁移 nullable 列无状态一致性约束

### 排程与业务（3 个）
- M10: Gantt API 无分页，大范围 OOM
- M11: BOM 服务 `ToListAsync()` 全租户加载
- M12: ProcessStep Sequence 重复无校验

### 租户与审计（3 个）
- M13: Read replica 5s sticky window 权限变更可能不一致
- M14: 审计日志实现 `ITenantEntity`，平台管理员无法跨租户查
- M15: 设备 Coordinator `IsPlatformAdmin=true` 未做 TenantId 二次校验

### 事实验证（2 个）
- M16: SSE 事件序列化失败可能破坏流
- M17: `VerifyAsync` 吞所有异常，安全异常无日志

### 国际化（2 个）
- M18: 非 en-US locale 全部回退 zh-CN，国际部署不优雅
- M19: 资源文件只 5 个 key，30+ 硬编码错误消息未本地化

### 部署与文档（5 个）
- M20: Compose 验证脚本不检 replica 专用卷
- M21: 安全文档要求 MQTT TLS 但无强制机制
- M22: 部署验证无验收标准
- M23: 密钥轮换生命周期无明确过程
- M24: Replication `samenet` 可能允许非预期子网

### 测试基础设施（6 个）
- M25: FactVerifier benchmark 初始化位置错误且无 DB 查询
- M26: InMemoryDatabaseRoot 字段共享可能泄露状态
- M27: Playwright 硬编码 tenant ID + mock API 响应
- M28: 缺失跨模块 E2E（Plan 要求但未实现）
- M29: Benchmark 不在 CI，无回归检测
- M30: 生成 TS 类型全 optional，后端为 non-null

---

## 低危问题（LOW）— 12 个

详见完整清单（M31-M42）：设备测试端点吞异常、Docker 健康检查缺 curl、租户切换不签发新 Token、appsettings 占位符无运行时检查、benchmark 合成数据不代表生产、空连接字符串等。

---

## 缺失测试与验收不足

1. **SignalR Hub 租户隔离**：无测试验证匿名/跨租户订阅被拒
2. **排程并发**：无并发调整操作测试
3. **迁移在非空库上执行**：集成测试从空库跑，未验证有数据时的唯一约束和 backfill
4. **设备 TLS/Auth**：无测试验证 MQTT 未配 TLS 时拒绝连接
5. **OPC UA 降级**：无测试验证空 thumbprint 时正确拒绝
6. **Data Protection 密钥丢失**：无故障注入测试
7. **跨租户 BOM 查询**：无测试验证 null tenant 时不崩溃
8. **Gantt 大范围查询**：无性能/边界测试
9. **Locale 端到端传播**：无测试验证 Accept-Language → Agent prompt 语言
10. **跨模块 E2E**：Plan 要求但未实现（BOM → 排程 → Agent 查询 → 验证结果）

---

## 修复优先级建议

### P0（合并前必修）
- B1-B14 全部阻塞级问题
- H1-H6（基础设施安全）
- H9、H11、H17、H20

### P1（上线前强烈建议）
- H7-H8、H10、H12-H16、H18-H19
- M1-M9

### P2（首个生产版本前）
- M10-M30
- 补充缺失测试 1-10

### P3（后续迭代）
- L1-L12

---

## 正面发现

- OPC UA `AutoAcceptUntrustedCertificates = false` + thumbprint pinning 正确
- Coordinator 实现指数退避
- Channel bounded with `DropOldest` 防止内存压力
- 审计日志结构完整
- 测试覆盖率达到 70.42% 门槛
- EF Core 租户过滤器架构合理
- 国际化基础设施（中间件、资源文件、双语模板）架构正确

---

## 后续行动

1. **立即修复**：B1-B14、H1-H6、H9、H11、H17、H20
2. **补充测试**：SignalR 租户隔离、排程并发、locale 传播、跨模块 E2E
3. **完善文档**：备份/恢复完整命令、验收标准、密钥轮换过程
4. **强化 CI**：覆盖率门槛、租户隔离冒烟测试
5. **安全加固**：Redis/MQTT 认证、TLS 强制、Data Protection 持久化

---

**审查完成时间**: 2026-07-10T15:30:00+08:00
**审查 agent 数量**: 9（并行执行）
**审查代码行数**: ~9k changed lines
**发现问题总数**: 65（15 BLOCKING + 20 HIGH + 30 MEDIUM + 12 LOW）
