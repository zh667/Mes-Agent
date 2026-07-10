# Phase 3 Deployment and Upgrade Runbook

## Topology

The default Compose topology contains PostgreSQL with pgvector, authenticated Redis, authenticated Mosquitto, the ASP.NET Core API, and Next.js. The device simulator is opt-in through the `simulation` profile. `docker-compose.replica.yml` adds a streaming read replica and enables read routing.

## Preflight

1. Create `.env` from `.env.example`, replace every `change-me` value with generated secrets, and set the initial `BOOTSTRAP_ADMIN_EMAIL` and `BOOTSTRAP_ADMIN_PASSWORD`. Remove the bootstrap values from later deployments after the first platform administrator exists.
2. Keep PostgreSQL, Redis, MQTT, Data Protection keys, and the OPC UA trust store on persistent storage.
3. Run `powershell -ExecutionPolicy Bypass -File scripts/verify-phase3-compose.ps1 -Production -EnvironmentFile .env`. This rejects missing, short, and placeholder database, replication, Redis, MQTT, JWT, audit HMAC, and NextAuth secrets.
4. Back up the Phase 2 database from the PostgreSQL container. The explicit host, port, user, and database prevent an ambient client configuration from selecting the wrong target:

   ```powershell
   docker compose exec -T postgres sh -c 'PGPASSWORD="$POSTGRES_PASSWORD" pg_dump --host=localhost --port=5432 --username="$POSTGRES_USER" --dbname="$POSTGRES_DB" --format=custom' > mes-copilot-phase2.dump
   ```

5. Verify the dump before the maintenance window:

   ```powershell
   docker compose exec -T postgres pg_restore --list < mes-copilot-phase2.dump | Select-Object -First 20
   ```

## Upgrade

1. Stop application writes and record the current image digest.
2. Run `dotnet ef migrations script --idempotent` from the reviewed release to produce the migration artifact.
3. Apply the script to a restored staging copy and run tenant backfill checks before production.
4. Start PostgreSQL, Redis, and MQTT; then start the API and wait for `/health` to report `Healthy`.
5. Start the web container. Enable `--profile simulation` only in test or demonstration environments.
6. Verify one login, tenant switch, BOM explosion, scheduling read, device registry read, Agent verification, and audit query.

## Tenant Backfill Checks

- Every tenant-owned row has a non-empty `TenantId`.
- Each active user has at least one active membership.
- Composite tenant foreign keys and unique constraints are present.
- A request without tenant context returns no business rows.

## Rollback

Do not run destructive down migrations after accepting Phase 3 writes. Stop the new application, restore the Phase 2 database backup to a new database, restore the previous image digest, and point the application to the restored database. Preserve audit logs and failed migration output as release evidence.

Restore into a newly created rollback database; do not overwrite the failed upgrade in place:

```powershell
docker compose exec -T postgres sh -c 'createdb --host=localhost --port=5432 --username="$POSTGRES_USER" mes_copilot_rollback && pg_restore --host=localhost --port=5432 --username="$POSTGRES_USER" --dbname=mes_copilot_rollback --clean --if-exists' < mes-copilot-phase2.dump
```

## Maintenance Windows

Tenant backfill and composite-index creation require an exclusive maintenance window sized from a staging copy of production. Keep the prior database and Data Protection key ring until refresh-token and session lifetimes have elapsed.

Rotate database, Redis, MQTT, JWT, audit HMAC, NextAuth, and replication credentials through the deployment secret store. JWT and NextAuth rotation must account for active token/session lifetime; Data Protection key rotation must retain old keys until all protected cookies have expired. Re-run the production Compose verification after every rotation.

## Release Evidence

- Production Compose verification exits successfully with the release `.env` and replica overlay.
- The backup lists successfully and restores into a disposable database.
- Database migrations apply to the restored staging copy and tenant backfill checks pass.
- Unit, integration, frontend, Playwright, coverage threshold, and short benchmark jobs are green for the release commit.
- Login, tenant switch, BOM explosion, schedule adjustment, device registry, Agent verification, and audit query complete under a non-default tenant.
