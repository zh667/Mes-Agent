# Tenant Isolation Boundary

- The authenticated identity proves the user; the active tenant header selects one membership already issued to that identity.
- Middleware validates membership before initializing `ITenantContext`.
- EF Core global filters are fail-closed when no tenant exists, and the write interceptor rejects missing or mismatched `TenantId` values.
- Tenant-owned relationships use composite tenant foreign keys so cross-tenant references fail in PostgreSQL.
- SignalR subscriptions validate resource ownership and include tenant IDs in group names.
- Platform administration does not disable business query filters. Cross-tenant audit aggregation is outside the current contract.

Tests must cover no tenant, invalid tenant, a valid member, another tenant's identifier, and tenant-mismatched writes.
