# Database Rules

Use these rules for EF Core, migrations, PostgreSQL or SQL Server, query design,
and traceability data.

## Database Direction

- Prefer PostgreSQL for new work, with `pgvector` if the RAG store lives in the
  same database.
- SQL Server is acceptable when the project targets a Microsoft-heavy
  manufacturing environment.
- Use EF Core migrations for schema changes.
- Keep migration names descriptive: `AddWorkOrderStatusFlow`,
  `CreateQualityTraceTables`.

## Modeling

Core tables should support:

- Products, materials, BOMs.
- Routings, operations, workstations, production lines.
- Work orders, job cards, production reports.
- Quality inspections, defect records, rework, scrap.
- Equipment, equipment status, alarms, downtime reasons.
- SOP/document metadata and vector index references.

Traceability records must preserve:

- Batch number.
- Work order number.
- Operation and workstation.
- Operator.
- Equipment.
- Material lots when relevant.
- Timestamp.

## EF Core Query Rules

- Use `AsNoTracking()` for read-only queries.
- Project with `Select` instead of loading full entities for list views.
- Avoid N+1 queries.
- Use transactions for multi-entity state transitions.
- Use optimistic concurrency for work order and reporting flows that multiple
  users may update.
- Keep database access in Infrastructure or application services, not UI or Agent
  presentation code.

Example:

```csharp
var summary = await _dbContext.WorkOrders
    .AsNoTracking()
    .Where(w => w.Status == WorkOrderStatus.InProduction)
    .Select(w => new WorkOrderSummary
    {
        Id = w.Id,
        ProductName = w.Product.Name,
        CompletedQuantity = w.CompletedQuantity,
        PlannedQuantity = w.PlannedQuantity
    })
    .ToListAsync();
```

## Migrations

- Review generated migrations before committing.
- Do not put seed data with secrets, real customer data, or private production
  data in migrations.
- Prefer small migrations tied to one feature.
- Include rollback considerations in PR notes for risky schema changes.

## RAG Storage

- Store source documents such as SOPs, process files, maintenance manuals, and
  quality standards.
- Chunk size target: 500 to 1000 tokens.
- Persist metadata: file name, document type, section, updated time, source hash,
  and access level.
- Do not store real production data in the knowledge base unless the privacy and
  retention model has been explicitly designed.
