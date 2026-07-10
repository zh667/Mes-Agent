# MES Copilot Phase 3 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: use `executing-plans` to implement this plan task by task. The repository has one writing agent only. Track every step with the checkbox syntax below and stop at each review checkpoint.

**Goal:** 将现有 MES Copilot 从已具备认证、流式 Agent、RAG、报表和 Docker 基线的应用，升级为具备租户隔离、可审计制造流程、可验证 Agent 输出、设备协议接入和中英双语能力的生产候选版本。

**Architecture:** 保留当前单体模块化架构和 `MesDbContext : IdentityDbContext<AppUser>`，通过显式租户上下文、数据库写入守卫和全局查询过滤实现共享库隔离。制造能力复用现有 BOM、工艺路线、工单工序、设备和 Agent 验证抽象；新增依赖均先经过兼容性验证，再通过 OpenAPI 生成前端类型。

**Tech Stack:** .NET 8、ASP.NET Core、EF Core 8、PostgreSQL 16 + pgvector、Redis、SignalR、Next.js 14、React 18、TypeScript、Vitest、xUnit、Testcontainers、Docker Compose。

## Current Baseline

本计划以 2026-07-10 工作区为准，实施前必须确认以下基线仍存在：

- `src/MesCopilot.Infrastructure/Data/MesDbContext.cs` 继承 `IdentityDbContext<AppUser>`。
- `src/MesCopilot.Domain/Entities/Products/Bom.cs` 和 `BomItem.cs` 已存在。
- `src/MesCopilot.Domain/Entities/Production/ProcessRoute.cs`、`ProcessStep.cs`、`WorkOrderOperation.cs` 已存在。
- `src/MesCopilot.Domain/Entities/Equipment/Equipment.cs` 和 `EquipmentStatus.cs` 已存在。
- `src/MesCopilot.Agent/Caching/IAgentResponseCache.cs` 已存在，当前是同步内存实现。
- `src/MesCopilot.Agent/Verification/IFactVerifier.cs`、`FactVerifier.cs` 和 `VerificationResult.cs` 已存在。
- `src/MesCopilot.Api/Controllers/AgentController.cs` 已输出 SSE 流并调用事实验证。
- 前端 API 类型由 `docs/openapi/mescopilot.v1.json` 生成到 `web/shared/api/generated/schema.ts`。
- 前端配置文件是 `web/next.config.mjs`，认证中间件是 `web/middleware.ts`。
- Docker Compose 当前包含 PostgreSQL、API、DeviceSimulator 和 Web。

如果任一基线不成立，先更新本计划中受影响的文件路径和接口，再写测试；不得凭旧计划创建重复类型。

## Design Corrections

本计划对原设计稿做以下落地修正，目标不变，但消除会破坏当前代码或制造虚假完成状态的方案：

| 原方案 | 本计划的落地方式 |
| --- | --- |
| 将 `MesDbContext` 改为第三方多租户基类 | 保留 Identity 继承，使用应用自有租户上下文、查询过滤和写入守卫 |
| 信任 `X-Tenant-Id` 请求头 | 请求头仅用于选择租户，服务端必须验证当前用户的有效成员关系 |
| 审计中保存原始请求体和连接字符串 | 只保存受控字段，认证令牌、密码、密钥和设备配置必须脱敏或省略 |
| 新建第二套 Agent 缓存接口 | 将现有 `IAgentResponseCache` 升级为异步、租户感知的实现 |
| 所有读取无条件走副本 | 仅陈旧容忍查询走副本；认证、写后读、验证和实时状态走主库 |
| 新建 BOM 和工序实体 | 扩展现有 `BomItem`、`WorkOrderOperation`、`ProcessStep` 和 `Equipment` |
| 持久化每次 BOM 计算结果 | BOM 展开结果默认即时计算；库存快照单独持久化 |
| 一次完成三个 PLC 协议 | 先完成 MQTT 垂直切片，再逐个接入 OPC UA 和 Modbus TCP |
| API 返回原始设备连接字符串 | 配置加密存储，API 只返回掩码摘要；本期协议操作只读 |
| 根据自然语言生成任意 SQL 验证 | 依据工具名和结构化结果调用白名单验证器及类型安全查询 |
| 新建第二套 `IFactVerifier` | 演进现有接口，并在 SSE、审计和 UI 中复用同一结果 DTO |
| 修改不存在的 `next.config.js` | 使用现有 `web/next.config.mjs`，不改变 App Router 基线 |

## Global Constraints

1. 每个任务严格执行 Red -> Green -> Refactor；先证明新测试因缺少行为而失败，再写实现。
2. 所有业务 API 必须认证；管理 API 使用明确策略，不能只在 UI 隐藏入口。
3. 租户由服务端成员关系解析。任何跨租户读写都必须返回 403 或 404，不能依赖客户端自律。
4. `TenantId` 在新增实体时由服务端赋值；修改和删除时校验原始租户值，禁止请求体覆盖。
5. 前端不得手写后端响应类型。每次 API DTO 变更都要刷新 Swagger 并运行 `pnpm --dir web generate:api`。
6. API 不返回 EF Core 实体、加密配置、数据库连接串、访问令牌、刷新令牌或内部异常消息。
7. 所有列表端点必须数据库侧分页，并有明确的 `take` 上限；禁止先全表加载再分页。
8. 所有新 EF 模型必须有配置、迁移、回滚检查和真实 PostgreSQL 集成测试。
9. 新依赖先在独立步骤执行 restore/build，最终项目文件和 lockfile 必须固定解析结果，不使用预发布包。
10. 数据库、Redis、MQTT 和协议模拟器集成测试必须使用隔离实例，不能依赖开发者机器中的持久数据。
11. 设备协议本期只读取和订阅，不向 PLC 写寄存器、变量或控制命令。
12. Agent 验证失败、未执行查询或查询异常时状态必须是 `Unverified` 或 `Disputed`，不能默认为已验证。
13. 每完成两个任务停止实施，输出变更、测试和剩余风险，等待用户代码审查。
14. 审查通过前不自动提交或推送；审查通过后先提出 Conventional Commit 信息。
15. 任务完成时不得遗留空实现、抛出未实现异常、伪造断言或仅增加覆盖率但不保护行为的测试。

## Shared Verification Commands

每个任务先运行自己的定向测试。每个审查点运行以下最小回归集：

```powershell
dotnet build MesCopilot.sln -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet format MesCopilot.sln --verify-no-changes --no-restore
pnpm --dir web lint
pnpm --dir web typecheck
pnpm --dir web test
git diff --check
```

涉及 Docker 或前端页面的审查点额外运行：

```powershell
pnpm --dir web build
docker compose config
docker compose build
```

---

## Sub-Phase 3A: Tenant-Safe Platform Foundation

### Task 3A.1: Tenant Model, Data Backfill, and Database Enforcement

**Outcome:** 所有现有业务数据归属一个明确租户；查询自动隔离，新增、修改和删除均受写入守卫保护，同时保持当前 Identity 上下文可用。

**Files:**

- Create: `src/MesCopilot.Domain/Common/ITenantEntity.cs`
- Create: `src/MesCopilot.Domain/Entities/Identity/Tenant.cs`
- Create: `src/MesCopilot.Domain/Entities/Identity/UserTenantMembership.cs`
- Create: `src/MesCopilot.Infrastructure/Tenancy/ITenantContext.cs`
- Create: `src/MesCopilot.Infrastructure/Tenancy/CurrentTenantContext.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Interceptors/TenantWriteGuardInterceptor.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Configurations/TenantConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/ProductConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/WorkOrderConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/QualityConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/EquipmentConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/KnowledgeConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/ConversationConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/SeedData.cs`
- Modify: `src/MesCopilot.Domain/Entities/Products/Product.cs`
- Modify: `src/MesCopilot.Domain/Entities/Products/Material.cs`
- Modify: `src/MesCopilot.Domain/Entities/Products/Bom.cs`
- Modify: `src/MesCopilot.Domain/Entities/Products/BomItem.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/ProductionLine.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/ProcessRoute.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/ProcessStep.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/Workstation.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/WorkOrder.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/WorkOrderOperation.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/ProductionReport.cs`
- Modify: `src/MesCopilot.Domain/Entities/Quality/QualityInspection.cs`
- Modify: `src/MesCopilot.Domain/Entities/Quality/DefectRecord.cs`
- Modify: `src/MesCopilot.Domain/Entities/Quality/DefectType.cs`
- Modify: `src/MesCopilot.Domain/Entities/Equipment/Equipment.cs`
- Modify: `src/MesCopilot.Domain/Entities/Equipment/EquipmentStatus.cs`
- Modify: `src/MesCopilot.Domain/Entities/Equipment/EquipmentAlarm.cs`
- Modify: `src/MesCopilot.Domain/Entities/Equipment/DowntimeRecord.cs`
- Modify: `src/MesCopilot.Domain/Entities/Knowledge/Document.cs`
- Modify: `src/MesCopilot.Domain/Entities/Knowledge/DocumentVersion.cs`
- Modify: `src/MesCopilot.Domain/Entities/Knowledge/DocumentChunk.cs`
- Modify: `src/MesCopilot.Domain/Entities/Conversations/Conversation.cs`
- Modify: `src/MesCopilot.Domain/Entities/Conversations/ConversationMessage.cs`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddTenantIsolation`
- Test: `tests/MesCopilot.UnitTests/Infrastructure/Tenancy/TenantWriteGuardInterceptorTests.cs`
- Test: `tests/MesCopilot.UnitTests/Infrastructure/Data/TenantQueryFilterTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Tenancy/TenantIsolationDatabaseTests.cs`

**Interfaces:**

```csharp
public interface ITenantEntity
{
    string TenantId { get; set; }
}

public interface ITenantContext
{
    string? TenantId { get; }
    bool IsPlatformAdmin { get; }
}

public sealed record TenantResolution(string TenantId, bool IsPlatformAdmin);
```

`CurrentTenantContext` 提供一次性 `Initialize(TenantResolution resolution)`；第二次初始化必须抛出 `InvalidOperationException`，避免同一请求切换上下文。

- [ ] **Step 1: 建立失败测试。** 覆盖租户 A 看不到租户 B 的工单、设备、文档和会话；新增实体自动写入当前租户；篡改 `TenantId` 的更新和删除抛出 `TenantBoundaryViolationException`；缺少租户上下文时业务写入失败；使用 raw SQL 插入跨租户父子外键时数据库拒绝。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~TenantWriteGuardInterceptorTests|FullyQualifiedName~TenantQueryFilterTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: FAIL，因为租户类型、过滤和写入守卫尚不存在。

- [ ] **Step 3: 实现租户模型。** `Tenant.Id` 使用 GUID 字符串；`Tenant.Code` 大写规范化并唯一；`UserTenantMembership` 使用 `(UserId, TenantId)` 唯一索引，包含 `UserRole Role`、`IsActive` 和 `CreatedAt`。
- [ ] **Step 4: 将所有持久化业务实体实现 `ITenantEntity`。** 子表也保存 `TenantId`；租户内实体建立 `(TenantId, Id)` alternate key，所有租户内父子关系改为 `(TenantId, ForeignId)` composite foreign key，数据库与写入守卫共同阻止跨租户引用；Identity 表、`Tenant` 和 `UserTenantMembership` 不应用租户查询过滤。
- [ ] **Step 5: 在 `MesDbContext` 中应用参数化全局过滤。** 普通请求仅匹配 `ITenantContext.TenantId`；平台管理员只有在明确的管理查询服务中使用 `IgnoreQueryFilters()`，普通仓储不暴露该能力。
- [ ] **Step 6: 实现写入守卫。** Added 状态由当前上下文赋值；Modified/Deleted 状态比较原始值、当前值和上下文；审计实体由后续任务单独处理。
- [ ] **Step 7: 生成迁移并人工审查 SQL。** 先插入固定默认租户 `00000000-0000-0000-0000-000000000001`，再回填现有业务数据和用户成员关系；成员角色复制当前 `AppUser.Role`；最后设置非空约束和 composite foreign key。现有业务编码唯一索引改为 `(TenantId, Code)` 等租户复合唯一索引。

```powershell
dotnet ef migrations add AddTenantIsolation --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
dotnet ef migrations script --idempotent --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api --output artifacts\phase3-tenant-migration.sql
```

- [ ] **Step 8: 运行真实 PostgreSQL 集成测试。** 验证迁移后现有行均属于默认租户，跨租户查询为空，跨租户写入失败，复合唯一索引允许不同租户使用相同业务编码。

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~TenantIsolationDatabaseTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 没有任何业务实体缺少 `TenantId`；没有使用第二个 DbContext 基类；迁移可从当前 Phase 2 数据库升级并可生成幂等脚本。

### Task 3A.2: Tenant Membership, Authorization, API, and Frontend Selection

**Outcome:** 登录用户只能选择自己所属的租户；租户角色参与现有授权策略；前端自动携带经过服务端验证的租户选择。

**Files:**

- Modify: `src/MesCopilot.Domain/Entities/Identity/AppUser.cs`
- Modify: `src/MesCopilot.Infrastructure/Identity/TokenService.cs`
- Create: `src/MesCopilot.Api/Middleware/TenantResolutionMiddleware.cs`
- Create: `src/MesCopilot.Api/Dtos/Tenants/TenantDtos.cs`
- Create: `src/MesCopilot.Api/Controllers/TenantsController.cs`
- Modify: `src/MesCopilot.Api/Controllers/AuthController.cs`
- Modify: `src/MesCopilot.Api/Program.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/IdentityConfiguration.cs`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddTenantAuthorization`
- Create: `web/lib/tenant.ts`
- Create: `web/components/tenant-selector.tsx`
- Create: `web/app/admin/tenants/page.tsx`
- Modify: `web/lib/api-client.ts`
- Modify: `web/lib/auth.ts`
- Modify: `web/components/providers.tsx`
- Modify: `web/app/layout.tsx`
- Test: `tests/MesCopilot.IntegrationTests/Tenancy/TenantAuthorizationTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/TenantsControllerTests.cs`
- Test: `web/components/tenant-selector.test.tsx`
- Test: `web/lib/tenant.test.ts`

**Authorization contract:**

- JWT 只保存用户标识、`jti` 和平台管理员声明，不保存可被租户切换绕过的全局业务角色。
- `TenantResolutionMiddleware` 在 `UseAuthentication()` 之后、`UseAuthorization()` 之前运行。
- 中间件读取 `X-Tenant-Id`，查询有效成员关系，再把该成员关系的 `UserRole` 添加到当前请求 principal。
- 业务 API 缺少租户选择返回 `400` 和稳定错误码 `TENANT_REQUIRED`；无成员关系返回 `403` 和 `TENANT_FORBIDDEN`。
- `/health`、Swagger 和 `/api/auth/login|register|refresh` 不要求租户头；`/api/auth/me` 返回可选租户列表。

- [ ] **Step 1: 建立失败的授权和前端测试。** 覆盖无头、非法租户、停用成员关系、正确租户角色、平台管理员租户管理、前端切换后 header 更新和无效本地选择回退。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~TenantAuthorizationTests|FullyQualifiedName~TenantsControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- tenant-selector.test.tsx tenant.test.ts
```

Expected: FAIL，因为成员关系解析和租户 UI 尚不存在。

- [ ] **Step 3: 完成身份模型。** `AppUser` 增加 `IsPlatformAdmin` 并移除已完成回填的全局 `Role`；普通授权只读取 `UserTenantMembership.Role`。迁移不得自动提升所有旧 Admin，首个平台管理员只由 `BootstrapAdminEmail` 配置和 SeedData 明确创建。
- [ ] **Step 4: 实现租户 API。** `GET /api/tenants/current`、`GET /api/tenants/mine`、`POST /api/tenants/switch/{tenantId}` 对成员开放；租户 CRUD 和成员新增/改角色/停用仅 `RequirePlatformAdmin`；删除行为是停用而非物理删除。Production 默认关闭匿名注册，用户由平台管理员创建后加入成员关系。
- [ ] **Step 5: 实现中间件和策略。** 不接受请求体中的 `TenantId`；控制器通过当前租户上下文获取租户；后台任务必须显式创建租户 scope。
- [ ] **Step 6: 实现前端租户选择。** 将选择保存到 `localStorage` 的 `mes.activeTenantId`，Axios request interceptor 添加 header；服务端 403 后清空选择并回到租户选择状态，不自动尝试其他租户。
- [ ] **Step 7: 生成授权迁移并刷新契约。** 迁移移除全局 Role 列并增加 platform admin 标记；Swagger 中为所有租户业务端点声明 header 和 400/403 响应，生成 TypeScript 类型，前端只引用生成 DTO。

```powershell
dotnet ef migrations add AddTenantAuthorization --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
pnpm --dir web generate:api
rg -n "TenantSummaryDto|X-Tenant-Id|TENANT_REQUIRED" docs\openapi\mescopilot.v1.json web\shared\api\generated\schema.ts
```

- [ ] **Step 8: 运行定向与回归验证。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~Tenant" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- tenant-selector.test.tsx tenant.test.ts
pnpm --dir web typecheck
```

Expected: PASS。

**Acceptance:** 修改请求头不能越权；角色随租户成员关系变化；API 与前端使用同一份生成契约。

## Review Checkpoint R1

停止实施并提交审查材料：租户实体清单、迁移 SQL 摘要、跨租户读写测试、授权矩阵、OpenAPI diff 和前端 header 行为。建议审查通过后的提交信息：`feat(tenancy): enforce tenant isolation and selection`。

### Task 3A.3: HTTP and EF Core Audit Trail with Redaction

**Outcome:** 自动记录 API 操作和实体变更，同时保证密码、令牌、连接配置和超大请求体不会进入审计库。

**Files:**

- Create: `src/MesCopilot.Domain/Entities/Auditing/AuditLog.cs`
- Create: `src/MesCopilot.Domain/Entities/Auditing/DataChangeLog.cs`
- Create: `src/MesCopilot.Application/Dtos/Auditing/AuditLogDto.cs`
- Create: `src/MesCopilot.Application/Services/Auditing/IAuditLogWriter.cs`
- Create: `src/MesCopilot.Infrastructure/Auditing/AuditRedactor.cs`
- Create: `src/MesCopilot.Infrastructure/Auditing/AuditLogWriter.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Interceptors/DataChangeAuditInterceptor.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Configurations/AuditConfiguration.cs`
- Create: `src/MesCopilot.Api/Middleware/AuditMiddleware.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Modify: `src/MesCopilot.Api/Program.cs`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddAuditTrail`
- Test: `tests/MesCopilot.UnitTests/Infrastructure/Auditing/AuditRedactorTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Auditing/AuditMiddlewareTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Auditing/DataChangeAuditTests.cs`

**Redaction policy:** keys matching `password`, `secret`, `token`, `authorization`, `apiKey`, `connectionString`, `certificate`, and `privateKey` are replaced with `[REDACTED]`; `/api/auth/*` and device connection mutation endpoints never persist request bodies; sanitized bodies are capped at 16 KiB.

- [ ] **Step 1: 建立失败测试。** 测试必须通过真实 TestServer 调用中间件，并验证状态码、耗时、租户、用户、相关 ID、脱敏字段、16 KiB 上限，以及审计写入异常不改变原 API 响应。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~AuditMiddlewareTests|FullyQualifiedName~DataChangeAuditTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: FAIL，因为审计链路尚不存在。

- [ ] **Step 3: 实现 HTTP 审计。** 记录路由模板而非包含敏感值的原始 URL；保存 query key 列表而非所有 query value；使用独立 scope 写入，失败只记录服务器日志。
- [ ] **Step 4: 实现变更审计。** 审计实体实现 `ITenantEntity`；租户写入守卫先执行，变更审计随后记录实体类型、主键、变更类型和经过脱敏的 old/new JSON；排除审计实体自身；删除记录在提交前捕获原值。
- [ ] **Step 5: 增加索引和保留字段。** `(TenantId, Timestamp)`、`(TenantId, UserId, Timestamp)` 和 `(TenantId, EntityType, EntityId)`；表中不存储原始访问令牌或原始 IP，IP 使用应用密钥 HMAC 后的摘要。
- [ ] **Step 6: 生成迁移并运行真实数据库测试。**

```powershell
dotnet ef migrations add AddAuditTrail --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~Audit" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 认证和设备配置请求体不入库；跨租户无法查询审计数据；审计失败不破坏业务事务和 HTTP 响应。

### Task 3A.4: Agent Audit, Audit Query API, Export, and Admin UI

**Outcome:** 每次 Agent 执行可追溯到问题、工具链、验证结果和耗时；管理员可按租户分页查询并导出受控合规报告。

**Files:**

- Create: `src/MesCopilot.Domain/Entities/Auditing/AgentAuditLog.cs`
- Create: `src/MesCopilot.Application/Dtos/Auditing/AgentAuditLogDto.cs`
- Create: `src/MesCopilot.Application/Services/Auditing/IAgentAuditService.cs`
- Create: `src/MesCopilot.Application/Services/Auditing/AgentAuditService.cs`
- Create: `src/MesCopilot.Application/Services/Auditing/IAuditQueryService.cs`
- Create: `src/MesCopilot.Application/Services/Auditing/AuditQueryService.cs`
- Create: `src/MesCopilot.Api/Controllers/AuditController.cs`
- Modify: `src/MesCopilot.Api/Controllers/AgentController.cs`
- Modify: `src/MesCopilot.Application/Services/IReportExportService.cs`
- Modify: `src/MesCopilot.Application/Services/ReportExportService.cs`
- Create: `web/app/admin/audit/page.tsx`
- Create: `web/components/audit/audit-table.tsx`
- Test: `tests/MesCopilot.IntegrationTests/Auditing/AgentAuditTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/AuditControllerTests.cs`
- Test: `web/app/admin/audit/page.test.tsx`

**API contract:** `GET /api/audit/operations`、`/changes`、`/agent` 均使用 `skip`/`take`，`take` 范围 1-100；`GET /api/audit/report?format=xlsx|pdf` 最大导出 31 天且最多 10,000 行。

- [ ] **Step 1: 建立失败测试。** 覆盖成功工具调用、工具异常、客户端取消、验证结果、分页上限、租户隔离、管理员策略和导出脱敏。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~AgentAuditTests|FullyQualifiedName~AuditControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 接入 Agent 执行链。** 使用 `try/finally` 记录完成、失败或取消状态；存储工具名、命名查询 ID、参数脱敏后的查询模板摘要和结构化验证结果，不存储工具返回中的密钥、连接串、SQL 参数值或完整文档内容；内部异常只写服务器日志。
- [ ] **Step 4: 实现数据库侧筛选分页。** 所有筛选在 `IQueryable` 上完成后才 materialize；返回 DTO，不返回实体或审计内部 JSON 原文。
- [ ] **Step 5: 复用现有 Excel/PDF 生成能力。** 超过行数限制返回 400；PDF 截断时在文档中明确显示“显示 N / 总计 M 条”。
- [ ] **Step 6: 实现管理页面。** 使用生成类型、分页表格、日期范围和类别筛选；详情抽屉显示脱敏字段；导出按钮使用下载图标和明确文件格式菜单。
- [ ] **Step 7: 刷新 OpenAPI 并验证。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~Audit" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- admin/audit/page.test.tsx
pnpm --dir web typecheck
```

Expected: PASS。

**Acceptance:** Agent 审计覆盖成功、失败和取消；查询数据库侧分页；导出受日期和行数上限保护。

## Review Checkpoint R2

停止实施并提交审查材料：脱敏策略、审计数据样例、Agent 异常链路、查询 SQL/分页证明、导出限制和管理页截图。建议提交信息：`feat(audit): add redacted operation and agent audit trails`。

### Task 3A.5: Redis Cache and Immediate Access-Token Revocation

**Outcome:** Agent 回答和设备状态使用租户感知缓存；知识库更新可主动失效；登出后当前 access token 立即失效；Redis 故障行为明确且可观测。

**Files:**

- Modify: `src/MesCopilot.Agent/Caching/IAgentResponseCache.cs`
- Modify: `src/MesCopilot.Agent/Caching/InMemoryAgentResponseCache.cs`
- Create: `src/MesCopilot.Infrastructure/Caching/RedisAgentResponseCache.cs`
- Create: `src/MesCopilot.Infrastructure/Caching/IDeviceStatusCache.cs`
- Create: `src/MesCopilot.Infrastructure/Caching/RedisDeviceStatusCache.cs`
- Create: `src/MesCopilot.Infrastructure/Identity/IAccessTokenRevocationStore.cs`
- Create: `src/MesCopilot.Infrastructure/Identity/RedisAccessTokenRevocationStore.cs`
- Modify: `src/MesCopilot.Agent/Plugins/KnowledgeAgentPlugin/KnowledgeAgentPlugin.cs`
- Modify: `src/MesCopilot.Application/Services/KnowledgeService.cs`
- Modify: `src/MesCopilot.Api/Controllers/AuthController.cs`
- Modify: `src/MesCopilot.Api/Program.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Modify: `src/MesCopilot.Infrastructure/MesCopilot.Infrastructure.csproj`
- Modify: `docker-compose.yml`
- Modify: `.env.example`
- Test: `tests/MesCopilot.UnitTests/Agent/Caching/InMemoryAgentResponseCacheTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Caching/RedisCacheTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Auth/AccessTokenRevocationTests.cs`

**Interfaces:**

```csharp
public interface IAgentResponseCache
{
    Task<FunctionCallResult?> GetAsync(string tenantId, string scope, string key, CancellationToken cancellationToken);
    Task SetAsync(string tenantId, string scope, string key, FunctionCallResult value, TimeSpan ttl, CancellationToken cancellationToken);
    Task InvalidateScopeAsync(string tenantId, string scope, CancellationToken cancellationToken);
}
```

失效通过租户+scope 版本号完成，响应 key 包含版本号；禁止 Redis `KEYS` 和全库扫描。查询 key 保留原始大小写，仅 trim 并做 SHA-256 摘要。

- [ ] **Step 1: 建立失败测试。** 覆盖租户隔离、大小写不同 query、5 分钟 Agent TTL、10 秒设备 TTL、scope 失效、有界内存缓存最多 1,000 条、登出 `jti` 立即拒绝和 Redis 不可用健康状态。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~InMemoryAgentResponseCacheTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 做依赖兼容性门禁。** 添加 `Microsoft.Extensions.Caching.StackExchangeRedis`，立即 restore/build；若出现 .NET 8 或 Npgsql 冲突，移除包并记录命令输出，不能继续堆叠依赖。

```powershell
dotnet add src\MesCopilot.Infrastructure\MesCopilot.Infrastructure.csproj package Microsoft.Extensions.Caching.StackExchangeRedis
dotnet restore MesCopilot.sln
dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 4: 实现异步缓存。** Production 环境缺少 Redis 配置时启动失败；Development/Test 可使用有界内存实现。序列化失败视为 miss 并记录结构化日志。
- [ ] **Step 5: 接入知识缓存失效。** 文档上传新版本、回滚和删除成功提交后递增 `knowledge` scope 版本；事务失败时不能提前失效。
- [ ] **Step 6: 实现 access-token 撤销。** Token 必须包含 `jti` 和过期时间；logout 将 `jti` 写入 Redis，TTL 等于剩余寿命；JWT 验证事件检查撤销状态。刷新 token 仍由数据库哈希轮换负责。
- [ ] **Step 7: 增加 Redis 容器和健康检查。** 使用 `redis:7-alpine`，不开公网端口作为默认；API 依赖 Redis healthy；连接串只来自环境变量。
- [ ] **Step 8: 运行 Testcontainers 集成测试。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~RedisCacheTests|FullyQualifiedName~AccessTokenRevocationTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
docker compose config
```

Expected: PASS。

**Acceptance:** 无跨租户缓存命中；知识变更后旧回答不再返回；登出后原 access token 被拒绝；不存在无上限内存字典。

### Task 3A.6: Configuration-Gated Read Routing and Replica Readiness

**Outcome:** 报表和审计等陈旧容忍查询可走 PostgreSQL standby；本地默认仍使用单库；写后读、事实验证、认证和实时状态强制主库。

**Files:**

- Create: `src/MesCopilot.Infrastructure/Data/ReadRouting/IReadDbContextFactory.cs`
- Create: `src/MesCopilot.Infrastructure/Data/ReadRouting/ReadDbContextFactory.cs`
- Create: `src/MesCopilot.Infrastructure/Data/ReadRouting/IReadConsistencyContext.cs`
- Create: `src/MesCopilot.Infrastructure/Data/ReadRouting/ReadConsistencyContext.cs`
- Create: `src/MesCopilot.Infrastructure/Data/ReadRouting/ReadRoutingOptions.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Modify: `src/MesCopilot.Application/Services/ReportExportService.cs`
- Modify: `src/MesCopilot.Application/Services/Auditing/AuditQueryService.cs`
- Modify: `src/MesCopilot.Api/appsettings.json`
- Modify: `.env.example`
- Create: `docker-compose.replica.yml`
- Create: `docker/postgres-replica/replica-entrypoint.sh`
- Test: `tests/MesCopilot.UnitTests/Infrastructure/Data/ReadRouting/ReadDbContextFactoryTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Data/ReadRoutingTests.cs`

**Routing rules:**

- `ConnectionStrings:MesDatabase` 始终是主库。
- `ConnectionStrings:MesReadDatabase` 仅在 `ReadRouting:Enabled=true` 时使用，可配置 Npgsql MultiHost 和 `Target Session Attributes=prefer-standby`。
- `IReadConsistencyContext.RequirePrimary(TimeSpan.FromSeconds(5))` 在写操作后设置；同一用户请求链的读取在窗口内走主库。
- 副本连接异常时只对幂等读重试一次主库；不重试写操作。

- [ ] **Step 1: 建立失败测试。** 覆盖默认单库、启用副本、写后读 sticky、事实验证固定主库、standby 失败回退和租户上下文传递。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~ReadDbContextFactoryTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 实现工厂。** 工厂创建同一 `MesDbContext` 类型并传入租户上下文和写入拦截器；禁止无效的 `DbContextOptions` 强制转换，也不创建第二套 EF 模型。
- [ ] **Step 4: 仅迁移陈旧容忍查询。** 报表和审计查询使用 read factory；认证、租户成员关系、Agent 事实验证、Knowledge 写后读和 Equipment 实时状态继续注入主 `MesDbContext`。
- [ ] **Step 5: 添加可选副本拓扑。** `docker-compose.replica.yml` 只在显式 compose overlay 时启动 standby；复制凭据来自环境变量；健康检查验证 `pg_is_in_recovery()`。
- [ ] **Step 6: 运行路由集成测试。** 测试必须从主/从连接的 `inet_server_addr()` 或注入的连接标签证明路由，而不是断言测试自身构造的布尔值。

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~ReadRoutingTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
docker compose -f docker-compose.yml -f docker-compose.replica.yml config
```

Expected: PASS；未启用 overlay 时主 compose 仍有效。

**Acceptance:** 本地单库零额外配置；副本只承担读；回退和写后读策略有测试证据；不承诺由应用代码控制的“复制延迟小于 1 秒”。

## Review Checkpoint R3

停止实施并提交审查材料：缓存 key/失效设计、Redis 故障行为、token 撤销测试、读路由矩阵、副本回退证据和 Docker diff。建议提交信息：`feat(platform): add tenant-aware redis cache and read routing`。

---

## Sub-Phase 3B: Manufacturing Deepening

### Task 3B.1: Multi-Level BOM Model, Inventory Snapshot, and Explosion Engine

**Outcome:** 复用现有 BOM 模型，支持多层子装配、循环检测、数量汇总和库存短缺计算，不持久化可重新计算的展开结果。

**Files:**

- Modify: `src/MesCopilot.Domain/Entities/Products/BomItem.cs`
- Create: `src/MesCopilot.Domain/Entities/Products/InventoryBalance.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/ProductConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`
- Create: `src/MesCopilot.Application/Dtos/Bom/BomExplosionDtos.cs`
- Create: `src/MesCopilot.Application/Services/Bom/IBomExplosionService.cs`
- Create: `src/MesCopilot.Application/Services/Bom/BomExplosionService.cs`
- Modify: `src/MesCopilot.Application/DependencyInjection.cs`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddMultiLevelBomAndInventory`
- Test: `tests/MesCopilot.UnitTests/Application/Services/Bom/BomExplosionServiceTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Bom/BomExplosionDatabaseTests.cs`

**Interface:**

```csharp
public interface IBomExplosionService
{
    Task<BomExplosionResultDto> ExplodeAsync(
        int productId,
        decimal quantity,
        CancellationToken cancellationToken = default);
}
```

`BomItem` 的 `MaterialId` 改为 nullable，并新增 `ChildBomId`；数据库 check constraint 保证两者恰好一个有值。`InventoryBalance.AvailableQuantity = max(0, QuantityOnHand - QuantityReserved)`。

- [ ] **Step 1: 建立失败测试。** 覆盖 3 层 BOM、同一物料多路径汇总、decimal 精度、无 BOM、停用 BOM、零/负数量、循环路径信息、深度超过 20、库存短缺和跨租户引用。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~BomExplosionServiceTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 扩展模型并迁移。** 现有行保留 `MaterialId`；新增子装配外键使用 Restrict 删除；每个租户和物料只有一个库存余额快照。
- [ ] **Step 4: 实现批量加载。** 一次加载租户内有效 BOM/Items/Materials 和涉及物料的库存，构建内存图后深度优先遍历；循环错误返回完整产品/BOM 路径；禁止递归查询数据库。
- [ ] **Step 5: 计算结果。** 每条叶子物料返回层级路径、需求量、可用量和短缺量；`totalMaterials` 按 MaterialId 汇总；不创建 `BomExplosionResult` 数据表。
- [ ] **Step 6: 运行单元和数据库测试。**

```powershell
dotnet ef migrations add AddMultiLevelBomAndInventory --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~BomExplosionServiceTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~BomExplosionDatabaseTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 3 层以上正确展开；循环不会栈溢出；数据库查询次数不随节点数线性增长；短缺来自真实库存快照而非硬编码 0。

### Task 3B.2: BOM API and Operations UI

**Outcome:** 操作员可通过认证 API 查看 BOM 树并计算物料需求；前端以可扫描的层级表和短缺摘要展示结果。

**Files:**

- Create: `src/MesCopilot.Api/Dtos/Bom/BomRequests.cs`
- Create: `src/MesCopilot.Api/Controllers/BomController.cs`
- Create: `web/app/scheduling/bom-explode/page.tsx`
- Create: `web/components/bom/bom-explosion-workspace.tsx`
- Create: `web/components/bom/bom-tree-table.tsx`
- Create: `web/lib/hooks/use-bom.ts`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/BomControllerTests.cs`
- Test: `web/app/scheduling/bom-explode/page.test.tsx`
- Test: `web/components/bom/bom-explosion-workspace.test.tsx`

**API contract:** `POST /api/bom/explode` body `{ productId, quantity }`，quantity 范围 `(0, 1_000_000]`；`GET /api/bom/{productId}/tree` 最大深度 20。Operator 可读/计算，库存修改不在本任务 API 范围内。

- [ ] **Step 1: 建立失败的 API 和组件测试。** 覆盖参数边界、404、循环冲突 409、租户隔离、树展开、短缺高亮、空库存状态和重复提交 loading 状态。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~BomControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- bom-explode/page.test.tsx bom-explosion-workspace.test.tsx
```

- [ ] **Step 3: 实现薄控制器。** 只负责认证、验证、状态码和 DTO；业务异常映射稳定错误码，不返回堆栈或实体。
- [ ] **Step 4: 刷新 OpenAPI 和生成类型。** 前端 hook 的请求/响应均从 `schema.ts` 推导。
- [ ] **Step 5: 实现运营工作区。** 使用数量输入、产品选择、层级表、短缺汇总和材料单位；卡片不嵌套，表头含 `scope="col"`，错误和 loading 状态不改变布局。
- [ ] **Step 6: 验证。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~BomControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- bom-explode/page.test.tsx bom-explosion-workspace.test.tsx
pnpm --dir web typecheck
```

Expected: PASS。

**Acceptance:** UI 不含手写后端 DTO；用户能看到每层来源路径、汇总需求和库存短缺；跨租户 productId 不泄露存在性。

## Review Checkpoint R4

停止实施并提交审查材料：BOM 数据模型、check constraint、循环测试、查询次数、OpenAPI diff 和桌面/移动页面截图。建议提交信息：`feat(bom): add tenant-safe multi-level explosion`。

### Task 3B.3: Sequential Scheduling Engine on Existing Work-Order Operations

**Outcome:** 在现有工艺路线和 `WorkOrderOperation` 上生成可持久化的顺序排程，考虑设备能力、已有占用和并发修改。

**Files:**

- Modify: `src/MesCopilot.Domain/Entities/Equipment/Equipment.cs`
- Modify: `src/MesCopilot.Domain/Entities/Production/WorkOrderOperation.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/EquipmentConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/WorkOrderConfiguration.cs`
- Create: `src/MesCopilot.Application/Dtos/Scheduling/SchedulingDtos.cs`
- Create: `src/MesCopilot.Application/Services/Scheduling/ISchedulingService.cs`
- Create: `src/MesCopilot.Application/Services/Scheduling/SequentialSchedulingService.cs`
- Modify: `src/MesCopilot.Application/DependencyInjection.cs`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddEquipmentScheduling`
- Test: `tests/MesCopilot.UnitTests/Application/Services/Scheduling/SequentialSchedulingServiceTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Scheduling/SchedulingConcurrencyTests.cs`

**Model decision:** `Equipment` 增加 nullable `WorkstationId`；`WorkOrderOperation` 增加 `EquipmentId` 和 PostgreSQL 并发 token。`PlannedStartTime`、`PlannedEndTime` 和 `Sequence` 继续作为排程真相，不新建重复 `ScheduleEntry` 表。

- [ ] **Step 1: 建立失败测试。** 覆盖工序顺序、两台候选设备选最早空闲、已有占用跳过、缺少有效路线、无候选设备、单次超过 50 工单、UTC 时间、相邻区间不冲突、并发生成只有一个提交成功。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~SequentialSchedulingServiceTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 扩展模型和迁移。** `(TenantId, EquipmentId, PlannedStartTime, PlannedEndTime)` 建索引；设备与工位使用 Restrict；历史 operation 的 EquipmentId 允许为空。
- [ ] **Step 4: 实现批量预取。** 一次加载工单、产品有效路线、步骤、候选设备和时间窗口内已有 operation；禁止循环中调用 `GetByIdAsync`。
- [ ] **Step 5: 实现顺序算法。** 工单按计划开始时间和 ID 稳定排序；每个步骤开始时间不早于前一步结束；选择最早可用设备；`StandardTime` 明确定义为每件分钟数，持续时间为 `max(1 minute, StandardTime * PlannedQuantity)`。只允许 NotScheduled/Scheduled 工单生成；重新生成只替换尚未开始的 planned operations，并在成功后把工单状态设为 Scheduled。
- [ ] **Step 6: 使用事务和并发 token 保存。** 冲突返回领域结果 `ScheduleConflict`，不吞掉 `DbUpdateConcurrencyException`；失败时不留下部分 operations。
- [ ] **Step 7: 验证。**

```powershell
dotnet ef migrations add AddEquipmentScheduling --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~SequentialSchedulingServiceTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~SchedulingConcurrencyTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 不重复创建已有实体；同一设备时间段不重叠；50 工单限制在服务层和 API 层均生效；并发失败可重试且无半成品。

### Task 3B.4: Scheduling API, Gantt Workspace, and Manual Adjustment

**Outcome:** TeamLead 可生成、查看和调整排程；甘特图支持指针拖动与键盘调整，并显示服务端冲突。

**Files:**

- Create: `src/MesCopilot.Api/Dtos/Scheduling/SchedulingRequests.cs`
- Create: `src/MesCopilot.Api/Controllers/SchedulingController.cs`
- Create: `web/app/scheduling/page.tsx`
- Create: `web/components/scheduling/scheduling-workspace.tsx`
- Create: `web/components/scheduling/gantt-timeline.tsx`
- Create: `web/lib/hooks/use-scheduling.ts`
- Modify: `web/package.json`
- Modify: `web/pnpm-lock.yaml`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/SchedulingControllerTests.cs`
- Test: `web/components/scheduling/gantt-timeline.test.tsx`
- Test: `web/components/scheduling/scheduling-workspace.test.tsx`
- Create: `web/e2e/scheduling.spec.ts`

**API contract:** `POST /api/scheduling/generate`、`GET /api/scheduling/gantt`、`GET /api/scheduling/workorders/{id}`、`PUT /api/scheduling/operations/{id}`。生成和修改要求 TeamLead；读取允许 Operator。调整请求包含并发 token，冲突返回 409。

- [ ] **Step 1: 建立失败测试。** 覆盖策略白名单、50 工单限制、跨租户、并发 token、拖动后调用 API、键盘左右调整、冲突回滚和时区显示。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~SchedulingControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- gantt-timeline.test.tsx scheduling-workspace.test.tsx
```

- [ ] **Step 3: 做前端依赖门禁。** 使用 `@dnd-kit/core` 实现拖动；添加后立即执行 install、typecheck 和现有测试。依赖不兼容时移除并用原生 pointer events 加键盘按钮实现同一契约。

```powershell
pnpm --dir web add @dnd-kit/core
pnpm --dir web typecheck
pnpm --dir web test
```

- [ ] **Step 4: 实现控制器和契约生成。** Gantt DTO 返回设备行、UTC 时间范围、operation、状态和并发 token；不让前端自行拼装 EF 实体。
- [ ] **Step 5: 实现甘特图。** 使用固定行高和时间刻度；条目颜色同时有文本/图标状态，不只靠颜色；拖动预览不提交，释放后调用 PUT，409 时恢复原位置并显示冲突详情。
- [ ] **Step 6: 添加 E2E。** 安装并固定 `@playwright/test`，覆盖登录、选择租户、生成排程、拖动、冲突提示和 1280x800/390x844 截图；测试数据使用隔离租户。
- [ ] **Step 7: 验证。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~SchedulingControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- gantt-timeline.test.tsx scheduling-workspace.test.tsx
pnpm --dir web typecheck
pnpm --dir web exec playwright test e2e/scheduling.spec.ts
```

Expected: PASS，截图中无文本重叠、空白画布或横向页面溢出。

**Acceptance:** 甘特图数据来自生成 DTO；拖动和键盘均可调整；服务端而非前端负责最终冲突判断。

## Review Checkpoint R5

停止实施并提交审查材料：排程算法样例、SQL 查询数、并发测试、409 行为、OpenAPI diff、桌面/移动截图和 E2E 录像或 trace。建议提交信息：`feat(scheduling): add sequential planning and gantt workspace`。

### Task 3B.5: Secure Device Connection Model and MQTT Vertical Slice

**Outcome:** 管理员可保存加密的 MQTT 连接配置、测试连接并启停后台采集；模拟器发布状态，API 更新设备状态并经 SignalR 推送。

**Files:**

- Create: `src/MesCopilot.Domain/Entities/Equipment/DeviceConnection.cs`
- Create: `src/MesCopilot.Domain/Enums/DeviceProtocol.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/IDeviceConnectionSecretProtector.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/DataProtectionDeviceConnectionSecretProtector.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/IDeviceConnector.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/IDeviceConnectorFactory.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/Mqtt/MqttDeviceConnector.cs`
- Create: `src/MesCopilot.Application/Services/Devices/IDeviceConnectionService.cs`
- Create: `src/MesCopilot.Application/Services/Devices/DeviceConnectionService.cs`
- Create: `src/MesCopilot.Api/HostedServices/DeviceCollectionCoordinator.cs`
- Create: `src/MesCopilot.Api/HostedServices/DeviceCollectorWorker.cs`
- Create: `src/MesCopilot.Infrastructure/Data/Configurations/DeviceConnectionConfiguration.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/MesDbContext.cs`
- Modify: `src/MesCopilot.Infrastructure/DependencyInjection.cs`
- Create: `src/MesCopilot.Api/Dtos/Devices/DeviceConnectionDtos.cs`
- Create: `src/MesCopilot.Api/Controllers/DeviceConnectionsController.cs`
- Modify: `src/MesCopilot.DeviceSimulator/Program.cs`
- Create: `src/MesCopilot.DeviceSimulator/Workers/MqttEquipmentPublisher.cs`
- Modify: `docker-compose.yml`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddDeviceConnections`
- Test: `tests/MesCopilot.UnitTests/Infrastructure/Devices/DeviceConnectorFactoryTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Devices/MqttDeviceConnectorTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Devices/DeviceCollectorWorkerTests.cs`

**Connector contract:**

```csharp
public interface IDeviceConnector : IAsyncDisposable
{
    DeviceProtocol Protocol { get; }
    Task ConnectAsync(CancellationToken cancellationToken);
    IAsyncEnumerable<DeviceReading> SubscribeAsync(CancellationToken cancellationToken);
    Task DisconnectAsync(CancellationToken cancellationToken);
}
```

`DeviceConnection` 保存 `EncryptedConfiguration`、`IsEnabled`、`LastConnectedAt`、`LastErrorCode` 和租户/设备外键。DTO 只返回 host、port、topic 等非秘密摘要和 `hasCredentials`。

连接 API 包含 `GET/POST/PUT/DELETE /api/device-connections`、`POST /{id}/test|start|stop` 和 `GET /{id}/realtime`；realtime 从 10 秒设备缓存读取，缓存 miss 返回 204，不直接轮询 PLC。

- [ ] **Step 1: 建立失败测试。** 覆盖配置加密往返、API 不回显秘密、工厂解析 MQTT、连接取消、断线指数退避、启停真实影响 worker、消息映射到 EquipmentState、SignalR 推送和跨租户控制拒绝。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~DeviceConnectorFactoryTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 做依赖兼容性门禁。** 添加 MQTTnet，restore/build 后确认无预发布依赖；最终 csproj 记录解析出的稳定版本。

```powershell
dotnet add src\MesCopilot.Infrastructure\MesCopilot.Infrastructure.csproj package MQTTnet
dotnet add src\MesCopilot.Infrastructure\MesCopilot.Infrastructure.csproj package Microsoft.AspNetCore.DataProtection
dotnet add src\MesCopilot.DeviceSimulator\MesCopilot.DeviceSimulator.csproj package MQTTnet
dotnet restore MesCopilot.sln
dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 4: 实现加密模型和 API。** 使用 ASP.NET Core Data Protection；密钥环通过 volume 持久化；test/start/stop 端点仅 Admin；start/stop 更新数据库并通知 worker，不只是切换 UI 状态。
- [ ] **Step 5: 实现 worker 生命周期。** Worker 放在 API 项目，协调 Infrastructure connector、Application device service 和现有 SignalR hub，避免 Infrastructure 反向引用 API/Application。每个启用连接一个受控任务；连接失败按 1/2/4/8/30 秒上限退避；停止或应用关闭能取消；禁止 `async void` 和 `System.Threading.Timer` 回调。
- [ ] **Step 6: 完成 MQTT 模拟器。** Compose 增加 Mosquitto，仅容器网络开放；模拟器按设备 topic 发布 JSON state；测试使用独立 broker 容器。
- [ ] **Step 7: 生成迁移并运行集成测试。**

```powershell
dotnet ef migrations add AddDeviceConnections --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~MqttDeviceConnectorTests|FullyQualifiedName~DeviceCollectorWorkerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 数据库和 API 中没有明文凭据；start/stop 控制真实采集任务；MQTT 消息端到端更新状态并推送；本期无写 PLC 能力。

### Task 3B.6: OPC UA, Modbus TCP, Protocol Simulation, and Device Admin UI

**Outcome:** OPC UA 和 Modbus TCP 通过与 MQTT 相同的抽象接入，并用模拟器完成真实协议集成测试；管理员可在同一页面管理三种连接。

**Files:**

- Create: `src/MesCopilot.Infrastructure/Devices/OpcUa/OpcUaDeviceConnector.cs`
- Create: `src/MesCopilot.Infrastructure/Devices/Modbus/ModbusTcpDeviceConnector.cs`
- Modify: `src/MesCopilot.Infrastructure/Devices/IDeviceConnectorFactory.cs`
- Create: `src/MesCopilot.DeviceSimulator/Workers/OpcUaEquipmentServer.cs`
- Create: `src/MesCopilot.DeviceSimulator/Workers/ModbusEquipmentServer.cs`
- Create: `web/app/admin/device-connections/page.tsx`
- Create: `web/components/devices/device-connections-workspace.tsx`
- Create: `web/lib/hooks/use-device-connections.ts`
- Modify: `docker-compose.yml`
- Test: `tests/MesCopilot.IntegrationTests/Devices/OpcUaDeviceConnectorTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Devices/ModbusTcpDeviceConnectorTests.cs`
- Test: `web/app/admin/device-connections/page.test.tsx`
- Create: `web/e2e/device-connections.spec.ts`

- [ ] **Step 1: 建立失败测试。** OPC UA 覆盖受信证书、拒绝未知证书、订阅状态节点、取消和重连；Modbus 覆盖 unit id/register 映射、超时、断线重连和只读；UI 覆盖协议专属字段、秘密不回显和测试连接结果。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~OpcUaDeviceConnectorTests|FullyQualifiedName~ModbusTcpDeviceConnectorTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 做两次独立依赖门禁。** OPC UA 包通过 build 和模拟器测试后才添加 Modbus 包；任一协议失败不影响已完成协议。

```powershell
dotnet add src\MesCopilot.Infrastructure\MesCopilot.Infrastructure.csproj package OPCFoundation.NetStandard.Opc.Ua
dotnet add src\MesCopilot.DeviceSimulator\MesCopilot.DeviceSimulator.csproj package OPCFoundation.NetStandard.Opc.Ua
dotnet restore MesCopilot.sln
dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet add src\MesCopilot.Infrastructure\MesCopilot.Infrastructure.csproj package NModbus
dotnet add src\MesCopilot.DeviceSimulator\MesCopilot.DeviceSimulator.csproj package NModbus
dotnet restore MesCopilot.sln
dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 4: 实现 OPC UA 垂直切片。** Production 禁止自动接受未知证书；Development 也必须通过显式 simulator thumbprint 信任；只订阅配置白名单节点。
- [ ] **Step 5: 实现 Modbus 垂直切片。** 限制 host、port、unit id、寄存器范围和轮询频率；仅 function code 读操作；文档说明生产网络必须由防火墙/VPN 隔离。
- [ ] **Step 6: 实现管理页面。** 使用生成 DTO；配置表显示协议、设备、状态、最后连接和错误码；秘密字段只允许替换，不能读取；测试/启停按钮有明确 loading 和结果状态。
- [ ] **Step 7: 跑协议集成和 E2E。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~DeviceConnector" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- admin/device-connections/page.test.tsx
pnpm --dir web exec playwright test e2e/device-connections.spec.ts
docker compose config
```

Expected: PASS。

**Acceptance:** 三协议都有真实模拟器集成测试；证书和凭据安全默认生效；UI 不展示明文配置；协议 worker 可取消、重连且不泄漏任务。

## Review Checkpoint R6

停止实施并提交审查材料：加密存储证明、协议依赖版本、连接状态机、重连日志、三协议集成测试、Docker 网络图和设备管理页截图。建议提交信息：`feat(devices): add secure mqtt opcua and modbus ingestion`。

---

## Sub-Phase 3C: Trustworthy Agent and Internationalization

### Task 3C.1: Typed L2 Fact Verification

**Outcome:** Agent 结构化工具结果通过工具专属、租户安全的主库查询重新验证；任何未执行或失败的检查都不会标成已验证。

**Files:**

- Modify: `src/MesCopilot.Agent/Verification/IFactVerifier.cs`
- Modify: `src/MesCopilot.Agent/Verification/FactVerifier.cs`
- Modify: `src/MesCopilot.Agent/Verification/VerificationResult.cs`
- Create: `src/MesCopilot.Agent/Verification/VerificationContext.cs`
- Create: `src/MesCopilot.Agent/Verification/IVerificationRule.cs`
- Create: `src/MesCopilot.Agent/Verification/Rules/DelayedOrdersVerificationRule.cs`
- Create: `src/MesCopilot.Agent/Verification/Rules/OeeVerificationRule.cs`
- Create: `src/MesCopilot.Agent/Verification/Rules/QualityVerificationRule.cs`
- Create: `src/MesCopilot.Agent/Verification/Rules/KnowledgeCitationVerificationRule.cs`
- Create: `src/MesCopilot.Application/Services/Verification/IVerificationQueryService.cs`
- Create: `src/MesCopilot.Application/Services/Verification/VerificationQueryService.cs`
- Modify: `src/MesCopilot.Agent/DependencyInjection.cs`
- Modify: `src/MesCopilot.Api/Controllers/AgentController.cs`
- Create: `src/MesCopilot.Api/Dtos/Agent/VerificationResultDto.cs`
- Test: `tests/MesCopilot.UnitTests/Agent/Verification/TypedFactVerifierTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Agent/FactVerificationDatabaseTests.cs`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/AgentVerificationStreamTests.cs`

**Interface:**

```csharp
public interface IFactVerifier
{
    Task<VerificationResult> VerifyAsync(
        FunctionCallResult result,
        VerificationContext context,
        CancellationToken cancellationToken = default);
}
```

`VerificationStatus` 值为 `Verified`、`Disputed`、`Unverified`。`VerificationContext` 包含 tenantId、userId、agentMode、toolName、UTC 查询边界和 correlationId；验证查询固定走主库。

- [ ] **Step 1: 建立失败测试。** 覆盖数量一致/不一致、OEE decimal 容差、列表数量逻辑、百分比组成、知识引用存在性、跨租户数据、查询异常、零声明和客户端取消。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~TypedFactVerifierTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 演进现有 L1。** 保留解释文本与结构化 Data 的一致性检查作为第一层；没有可检查声明时返回 Unverified，不能沿用当前默认 true 行为。
- [ ] **Step 4: 实现工具白名单规则。** Agent 规则只依赖 `IVerificationQueryService`；该 Application service 使用类型安全、受租户过滤且固定走主库的查询。规则不直接访问 DbContext，不解析模型生成 SQL，也不接受任意表名、列名或 where 条件。
- [ ] **Step 5: 接入 SSE。** `tool_result` 后输出 `verification` event；查询失败输出 Unverified 和公开错误码，不暴露异常；审计记录同一 verification DTO。
- [ ] **Step 6: 运行数据库和 SSE 测试。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~FactVerificationDatabaseTests|FullyQualifiedName~AgentVerificationStreamTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

Expected: PASS。

**Acceptance:** 不存在自然语言到任意 SQL 的执行路径；查询异常不会产生 Verified；SSE、审计和 API 使用同一结果语义。

### Task 3C.2: Verification API Contract and Agent UI

**Outcome:** Agent 页面展示已验证、存疑和未验证状态，用户可查看每项检查；重新加载后可从消息验证 API 获取相同结果。

**Files:**

- Create: `src/MesCopilot.Api/Controllers/AgentVerificationController.cs`
- Modify: `src/MesCopilot.Domain/Entities/Conversations/ConversationMessage.cs`
- Modify: `src/MesCopilot.Infrastructure/Data/Configurations/ConversationConfiguration.cs`
- Modify: `src/MesCopilot.Application/Dtos/ConversationDto.cs`
- Modify: `src/MesCopilot.Application/Services/ConversationService.cs`
- Modify: `web/lib/hooks/use-agent-stream.ts`
- Create: `web/components/agent/verification-status.tsx`
- Modify: `web/components/agent/chat-interface.tsx`
- Test: `tests/MesCopilot.IntegrationTests/Controllers/AgentVerificationControllerTests.cs`
- Test: `web/lib/hooks/use-agent-stream.test.ts`
- Test: `web/components/agent/verification-status.test.tsx`
- Test: `web/components/agent/chat-interface.test.tsx`
- Generate: migration files under `src/MesCopilot.Infrastructure/Migrations/` with migration name `AddMessageVerification`

**API contract:** `GET /api/agent/messages/{messageId}/verification` 返回 `VerificationResultDto`；`POST /api/agent/messages/{messageId}/verification/recheck` 仅同租户 Admin 可触发白名单规则重查。SSE `verification` event 的 data 使用同一 DTO JSON 结构，所有接口对跨租户或非所有者请求返回 404。

- [ ] **Step 1: 建立失败测试。** 覆盖三种状态、详情展开、刷新恢复、畸形 SSE event 忽略、跨用户/跨租户 404、无结果 404 和屏幕阅读器文本。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~AgentVerificationControllerTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- use-agent-stream.test.ts verification-status.test.tsx chat-interface.test.tsx
```

- [ ] **Step 3: 持久化消息验证摘要并生成迁移。** 在 Agent 消息上保存结构化 JSON 和 schema version；服务层反序列化为 DTO，损坏数据返回 Unverified 而不是 500。

```powershell
dotnet ef migrations add AddMessageVerification --project src\MesCopilot.Infrastructure --startup-project src\MesCopilot.Api
```
- [ ] **Step 4: 刷新 OpenAPI。** 前端 verification data 类型从生成 schema 导入；SSE 解析器只负责 event framing，不重新定义业务字段。
- [ ] **Step 5: 实现 UI。** 使用图标+文本显示 Verified/Disputed/Unverified；详情包含 claim、claimed、actual、rule 和时间；无验证时不显示成功图标。
- [ ] **Step 6: 验证。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~AgentVerification" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
pnpm --dir web test -- use-agent-stream.test.ts verification-status.test.tsx chat-interface.test.tsx
pnpm --dir web typecheck
```

Expected: PASS。

**Acceptance:** UI 不把“没有验证”显示为成功；刷新后结果一致；越权访问不泄露消息是否存在。

## Review Checkpoint R7

停止实施并提交审查材料：验证规则矩阵、失败关闭测试、主库查询证明、SSE 示例、审计记录和三种 UI 状态截图。建议提交信息：`feat(agent): add typed fact verification and status UI`。

### Task 3C.3: Frontend Internationalization Without Route Churn

**Outcome:** 现有和 Phase 3 前端页面支持 `zh-CN`、`en-US` 切换，语言通过 cookie 持久化，不破坏 NextAuth 路由保护。

**Files:**

- Modify: `web/package.json`
- Modify: `web/pnpm-lock.yaml`
- Modify: `web/next.config.mjs`
- Create: `web/i18n/request.ts`
- Create: `web/lib/locale.ts`
- Create: `web/messages/zh-CN.json`
- Create: `web/messages/en-US.json`
- Create: `web/components/language-switcher.tsx`
- Modify: `web/app/layout.tsx`
- Modify: `web/app/page.tsx`
- Modify: `web/app/login/page.tsx`
- Modify: `web/app/agent/page.tsx`
- Modify: `web/app/workorders/page.tsx`
- Modify: `web/app/equipment/page.tsx`
- Modify: `web/app/quality/page.tsx`
- Modify: `web/app/knowledge/page.tsx`
- Modify: `web/components/providers.tsx`
- Modify: `web/components/module-placeholder.tsx`
- Modify: `web/components/operations/operations-page-shell.tsx`
- Modify: `web/components/agent/chat-interface.tsx`
- Modify: `web/components/agent/verification-status.tsx`
- Modify: `web/components/tenant-selector.tsx`
- Modify: `web/app/admin/tenants/page.tsx`
- Modify: `web/app/admin/audit/page.tsx`
- Modify: `web/components/audit/audit-table.tsx`
- Modify: `web/app/scheduling/bom-explode/page.tsx`
- Modify: `web/components/bom/bom-explosion-workspace.tsx`
- Modify: `web/components/bom/bom-tree-table.tsx`
- Modify: `web/app/scheduling/page.tsx`
- Modify: `web/components/scheduling/scheduling-workspace.tsx`
- Modify: `web/components/scheduling/gantt-timeline.tsx`
- Modify: `web/app/admin/device-connections/page.tsx`
- Modify: `web/components/devices/device-connections-workspace.tsx`
- Test: `web/lib/locale.test.ts`
- Test: `web/components/language-switcher.test.tsx`
- Test: `web/app/layout.test.tsx`
- Create: `web/e2e/i18n.spec.ts`

**Locale strategy:** URL 不增加 locale 前缀；`MES_LOCALE` cookie 值只允许 `zh-CN` 或 `en-US`；缺失时按 `Accept-Language` 选择，仍缺失则 `zh-CN`。保留现有 `web/middleware.ts` 的 NextAuth matcher。

- [ ] **Step 1: 建立失败测试。** 覆盖 cookie、Accept-Language、非法 locale、缺 key 回退、切换持久化、日期/数字格式和受保护路由仍跳登录。
- [ ] **Step 2: 运行红灯。**

```powershell
pnpm --dir web test -- locale.test.ts language-switcher.test.tsx layout.test.tsx
```

- [ ] **Step 3: 做 next-intl 依赖门禁。** 安装后立即执行 typecheck/build；使用兼容 Next.js 14 App Router 的配置，不改名为 `next.config.js`。

```powershell
pnpm --dir web add next-intl
pnpm --dir web typecheck
pnpm --dir web build
```

- [ ] **Step 4: 建立消息目录。** key 按 `common`、`navigation`、`auth`、`agent`、`workorders`、`equipment`、`quality`、`knowledge`、`audit`、`bom`、`scheduling`、`devices` 分组；两份 JSON key 集完全相同。
- [ ] **Step 5: 迁移可见文字。** 使用 `rg` 扫描页面和组件中的硬编码中英文；业务数据、代码、设备编号和测试 fixture 不当作翻译 key。
- [ ] **Step 6: 实现语言切换。** 使用 globe 图标、原生 select/menu 和可访问标签；更新 cookie 后 `router.refresh()`；日期、数字和百分比使用 locale formatter。
- [ ] **Step 7: 运行 E2E。** 覆盖登录页、Agent、工单、审计、排程和设备页面，切换后刷新仍保持语言；页面无 hydration mismatch。

```powershell
pnpm --dir web test
pnpm --dir web typecheck
pnpm --dir web build
pnpm --dir web exec playwright test e2e/i18n.spec.ts
```

Expected: PASS。

**Acceptance:** 认证中间件行为不变；所有用户可见导航和页面标题使用 message key；中英文 key 集一致；格式跟随 locale。

### Task 3C.4: Backend Errors and Agent Prompt Localization

**Outcome:** API 通过稳定错误码和本地化 detail 响应；Agent system/user prompt 按请求语言选择模板，业务日志仍使用稳定英文结构字段。

**Files:**

- Create: `src/MesCopilot.Api/Localization/SharedResource.cs`
- Create: `src/MesCopilot.Api/Resources/SharedResource.zh-CN.resx`
- Create: `src/MesCopilot.Api/Resources/SharedResource.en-US.resx`
- Create: `src/MesCopilot.Api/Errors/ApiProblemFactory.cs`
- Modify: `src/MesCopilot.Api/Program.cs`
- Modify: `src/MesCopilot.Api/Controllers/AuthController.cs`
- Modify: `src/MesCopilot.Api/Controllers/AgentController.cs`
- Modify: `src/MesCopilot.Api/Controllers/DocumentsController.cs`
- Modify: `src/MesCopilot.Api/Controllers/EquipmentController.cs`
- Modify: `src/MesCopilot.Api/Controllers/QualityController.cs`
- Modify: `src/MesCopilot.Api/Controllers/ReportsController.cs`
- Modify: `src/MesCopilot.Api/Controllers/WorkOrdersController.cs`
- Modify: `src/MesCopilot.Api/Controllers/TenantsController.cs`
- Modify: `src/MesCopilot.Api/Controllers/AuditController.cs`
- Modify: `src/MesCopilot.Api/Controllers/BomController.cs`
- Modify: `src/MesCopilot.Api/Controllers/SchedulingController.cs`
- Modify: `src/MesCopilot.Api/Controllers/DeviceConnectionsController.cs`
- Modify: `src/MesCopilot.Api/Controllers/AgentVerificationController.cs`
- Modify: `src/MesCopilot.Agent/Prompts/IPromptBuilder.cs`
- Modify: `src/MesCopilot.Agent/Prompts/MesPromptBuilder.cs`
- Create: localized templates under `src/MesCopilot.Agent/Prompts/Templates/zh-CN/`
- Create: localized templates under `src/MesCopilot.Agent/Prompts/Templates/en-US/`
- Modify: `src/MesCopilot.Agent/MesCopilot.Agent.csproj`
- Test: `tests/MesCopilot.IntegrationTests/Localization/ApiLocalizationTests.cs`
- Test: `tests/MesCopilot.UnitTests/Agent/Prompts/LocalizedPromptBuilderTests.cs`

**Error contract:** ProblemDetails extension `code` 始终是稳定英文大写码；`title` 和 `detail` 可本地化；未知语言回退 `zh-CN`；内部日志不依赖本地化文本进行告警匹配。

- [ ] **Step 1: 建立失败测试。** 覆盖 `Accept-Language` 精确和降级匹配、未知语言、稳定 code、404/400/409/403、四种 Agent mode 的双语模板和模板缺失错误区分。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~LocalizedPromptBuilderTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~ApiLocalizationTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 配置 ASP.NET Core localization。** 支持 `zh-CN`、`en-US`，禁用 query-string culture provider，只接受 header；资源缺 key 时测试必须失败而不是将 key 返回用户。
- [ ] **Step 4: 统一 ProblemDetails。** 将本期和高频现有控制器错误迁移到 `ApiProblemFactory`；OpenAPI 为错误响应声明 ProblemDetails 和 `code`。
- [ ] **Step 5: 演进 PromptBuilder。** 接口增加 locale 参数；模板按 locale/mode lazy load 并缓存；用户问题保持原文，不翻译制造数据；审计记录使用实际 locale。
- [ ] **Step 6: 刷新契约并验证。**

```powershell
pnpm --dir web generate:api
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --filter "FullyQualifiedName~LocalizedPromptBuilderTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~ApiLocalizationTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
rg -n "ProblemDetails|code" docs\openapi\mescopilot.v1.json web\shared\api\generated\schema.ts
```

Expected: PASS。

**Acceptance:** 前端可依赖稳定错误码；本地化文本不参与程序分支；Agent 模板双语完整且找不到模板时不会误报为未知 mode。

## Review Checkpoint R8

停止实施并提交审查材料：中英文 key 对比、认证 middleware 回归、ProblemDetails 样例、Prompt 对比、E2E 截图和 OpenAPI diff。建议提交信息：`feat(i18n): localize web api errors and agent prompts`。

---

## Sub-Phase 3D: Deployment and Acceptance Gates

### Task 3D.1: Phase 3 Deployment Topology and Upgrade Runbook

**Outcome:** 新增 Redis、MQTT、协议模拟器和可选副本后，开发与生产配置边界清晰；Phase 2 数据库可按文档备份、升级和回滚。

**Files:**

- Modify: `docker-compose.yml`
- Modify: `docker-compose.replica.yml`
- Modify: `.env.example`
- Modify: `.gitignore`
- Create: `docs/deployment/phase3-deployment.md`
- Create: `docs/security/tenant-isolation.md`
- Create: `docs/security/device-credentials.md`
- Create: `scripts/verify-phase3-compose.ps1`
- Test: `tests/MesCopilot.IntegrationTests/Health/Phase3DependencyHealthTests.cs`

- [ ] **Step 1: 建立失败的依赖健康测试。** 覆盖 PostgreSQL、pgvector、Redis、MQTT、API 和可选 replica；协议模拟器由 profile 启动，不作为生产 API 健康的强依赖。
- [ ] **Step 2: 运行红灯。**

```powershell
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --filter "FullyQualifiedName~Phase3DependencyHealthTests" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 3: 整理 compose。** 所有服务有 healthcheck、restart policy、内部网络和命名 volume；数据库、Redis、MQTT 默认不映射公网端口；Development profile 才开放本机调试端口。
- [ ] **Step 4: 完成升级 runbook。** 文档包含备份命令、维护窗口、幂等 migration script、默认租户回填检查、逐项健康验证、失败回滚和恢复验证；不包含真实凭据。
- [ ] **Step 5: 完成安全文档。** 说明租户选择与授权边界、平台管理员能力、审计脱敏、Data Protection key ring、OPC UA trust store 和 Modbus 网络隔离。
- [ ] **Step 6: 编写只读验证脚本。** `verify-phase3-compose.ps1` 运行 config、检查必需环境变量名称、等待健康状态并调用 `/health`；脚本不删除 volume 或数据库。
- [ ] **Step 7: 验证。**

```powershell
docker compose config
docker compose -f docker-compose.yml -f docker-compose.replica.yml config
powershell -ExecutionPolicy Bypass -File scripts\verify-phase3-compose.ps1
```

Expected: PASS，脚本输出每个依赖的健康状态。

**Acceptance:** 无真实 secret；默认拓扑可启动；可选副本不会破坏默认拓扑；升级和回滚步骤可由另一位工程师独立执行。

### Task 3D.2: Security, Performance, Coverage, and End-to-End Release Gate

**Outcome:** 用可重复命令证明 Phase 3 的租户隔离、安全、性能、覆盖率和关键用户流程达到发布门槛。

**Files:**

- Create: `tests/MesCopilot.Benchmarks/MesCopilot.Benchmarks.csproj`
- Create: `tests/MesCopilot.Benchmarks/Program.cs`
- Create: `tests/MesCopilot.Benchmarks/BomExplosionBenchmarks.cs`
- Create: `tests/MesCopilot.Benchmarks/SchedulingBenchmarks.cs`
- Create: `tests/MesCopilot.Benchmarks/FactVerificationBenchmarks.cs`
- Modify: `MesCopilot.sln`
- Create: `web/e2e/phase3-smoke.spec.ts`
- Create: `docs/verification/phase3-release-evidence.md`
- Modify: `docs/superpowers/reviews/2026-07-09-phase2-review-followups.md` only to mark items actually resolved by Phase 3 with commit/test evidence

**Release thresholds:**

- BOM 10,000 节点内存图展开基准均值小于 500 ms，且无数据库 N+1。
- 50 个工单、每单 10 工序的顺序排程基准均值小于 2 s。
- 已准备数据上的类型化事实验证小于 3 s，不计 LLM 调用。
- Domain、Application、Agent 的 line coverage 合计不低于 70%，排除 migrations、生成代码、DI 和基础设施适配器。
- 所有租户隔离、凭据脱敏、授权策略和协议只读测试通过。

- [ ] **Step 1: 添加 BenchmarkDotNet 依赖并立即 build。**

```powershell
dotnet new console -n MesCopilot.Benchmarks -o tests\MesCopilot.Benchmarks --framework net8.0
dotnet sln MesCopilot.sln add tests\MesCopilot.Benchmarks\MesCopilot.Benchmarks.csproj
dotnet add tests\MesCopilot.Benchmarks\MesCopilot.Benchmarks.csproj package BenchmarkDotNet
dotnet build MesCopilot.sln -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
```

- [ ] **Step 2: 实现确定性基准数据。** 不访问外网或共享开发库；固定随机种子；基准分别隔离图构建、算法执行和数据库查询。
- [ ] **Step 3: 添加跨模块 E2E。** 覆盖登录、租户选择、跨租户拒绝、BOM 展开、排程、设备连接测试、Agent 验证、审计查询和语言切换。
- [ ] **Step 4: 运行完整后端验证。** 命令必须串行，避免 Windows `csc.exe` 文件锁。

```powershell
dotnet restore MesCopilot.sln
dotnet build MesCopilot.sln --no-restore -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --no-restore --logger "console;verbosity=normal" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet test tests\MesCopilot.IntegrationTests\MesCopilot.IntegrationTests.csproj --no-restore --logger "console;verbosity=normal" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet format MesCopilot.sln --verify-no-changes --no-restore
dotnet test tests\MesCopilot.UnitTests\MesCopilot.UnitTests.csproj --no-restore --settings tests\MesCopilot.UnitTests\coverage.runsettings --collect:"XPlat Code Coverage" -m:1 -p:BuildInParallel=false -p:UseSharedCompilation=false
dotnet run --project tests\MesCopilot.Benchmarks\MesCopilot.Benchmarks.csproj -c Release
```

- [ ] **Step 5: 运行完整前端验证。**

```powershell
pnpm --dir web install --frozen-lockfile
pnpm --dir web generate:api
pnpm --dir web lint
pnpm --dir web typecheck
pnpm --dir web test
pnpm --dir web build
pnpm --dir web exec playwright test e2e/phase3-smoke.spec.ts
```

- [ ] **Step 6: 运行容器和 diff 验证。**

```powershell
docker compose config
docker compose build
docker compose up -d
docker compose ps
git diff --check
rg -n "password|refreshToken|accessToken|connectionString|apiKey|privateKey" src web docs --glob "!docs/openapi/mescopilot.v1.json" --glob "!web/shared/api/generated/schema.ts"
```

检查搜索结果，仅允许字段名、脱敏规则、测试 fixture 和环境变量模板；发现真实值立即停止发布并轮换凭据。

- [ ] **Step 7: 写发布证据。** `phase3-release-evidence.md` 记录每条命令、日期、退出码、测试数量、覆盖率、基准摘要、Docker 健康状态和未关闭风险；不得用手写“通过”替代命令输出。
- [ ] **Step 8: 复核 follow-ups。** 只有被代码和测试真实解决的 Phase 2 条目才能标记完成；其余保留原状态和证据链接。

**Acceptance:** 所有命令退出码为 0；指标达到阈值；没有真实 secret；E2E 完成核心流程；发布证据可复现。

## Review Checkpoint R9

停止实施并提交最终审查材料：升级 runbook、完整命令结果、覆盖率、BenchmarkDotNet 摘要、E2E trace、Docker 健康状态、安全扫描和仍存在的风险。建议提交信息：`chore(release): add phase 3 deployment and verification gates`。

---

## Scope Exclusions

以下内容不属于 Phase 3 完成标准：

- 移动端 App、分库分表、多区域部署和 CDN。
- 完整 MRP/ERP、采购建议和供应商计划。
- 遗传算法、约束求解器或自动优化 APS。
- 真实工厂 PLC 上线和写控制指令。
- Agent 人工审批工作流。
- 中英文之外的语言。
- 任意自然语言生成 SQL 并直接执行。
- 将所有查询强制路由到从库。

## Final Self-Review Checklist

- [ ] 每个新类型只有一个创建任务，后续任务只修改或消费它。
- [ ] 计划中的现有文件路径均在当前仓库中验证过。
- [ ] `MesDbContext` 始终保留 Identity 继承。
- [ ] BOM、ProcessStep、WorkOrderOperation、Equipment、`IAgentResponseCache` 和 `IFactVerifier` 均复用现有实现。
- [ ] 每个 EF 模型变更都有迁移、回滚审查和 PostgreSQL 集成测试。
- [ ] 每个 API 变更都有 OpenAPI 刷新和生成 TypeScript 类型步骤。
- [ ] 所有分页、导出、BOM、排程和连接参数都有服务端上限。
- [ ] 所有租户选择都经过成员关系验证，写入守卫覆盖 Added/Modified/Deleted。
- [ ] 审计、缓存和设备配置没有明文 secret 路径。
- [ ] Agent 验证失败关闭，且没有模型生成任意 SQL 的执行路径。
- [ ] PLC 连接只读，三协议分别有真实模拟器集成测试。
- [ ] i18n 不破坏 NextAuth middleware，错误码不随语言变化。
- [ ] 每两个任务都有明确的审查停止点。
- [ ] 最终验证包含 lint、typecheck、unit、integration、build、format、E2E、Docker 和 diff 检查。

**Plan status:** Ready for review. Implementation must begin with Task 3A.1 and stop after Task 3A.2 for Review Checkpoint R1.
