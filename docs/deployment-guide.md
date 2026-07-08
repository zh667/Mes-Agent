# MES Copilot Deployment Guide

This guide covers local and staging deployment for the Phase 1 MES Copilot stack.

## Prerequisites

- Docker Desktop or Docker Engine with Compose v2.
- .NET SDK 8.0 or later for local backend development.
- Node.js 18 or later and pnpm for local frontend development.
- PostgreSQL 16 with pgvector when running outside Docker Compose.

## Services

| Service | Container | Default URL | Purpose |
| --- | --- | --- | --- |
| PostgreSQL | `mes-copilot-postgres` | `localhost:5432` | MES relational data and pgvector storage |
| API | `mes-copilot-api` | `http://localhost:5000` | ASP.NET Core REST API and SignalR hub |
| Device Simulator | `mes-copilot-simulator` | N/A | Background equipment status and production event simulation |
| Web | `mes-copilot-web` | `http://localhost:3000` | Next.js operations console |

## Environment Variables

Copy `.env.example` to `.env` for local Compose runs and adjust values as needed.

| Variable | Default | Description |
| --- | --- | --- |
| `POSTGRES_DB` | `mes_copilot` | Database name created by the PostgreSQL container |
| `POSTGRES_USER` | `postgres` | PostgreSQL user |
| `POSTGRES_PASSWORD` | `postgres` | Local-only password; change before any shared environment |
| `POSTGRES_PORT` | `5432` | Host port for PostgreSQL |
| `API_PORT` | `5000` | Host port for the ASP.NET Core API |
| `WEB_PORT` | `3000` | Host port for the Next.js app |
| `NEXT_PUBLIC_API_URL` | `http://localhost:5000/api` | Browser-facing API base URL used by the frontend |

The API and simulator use `ConnectionStrings__MesDatabase` inside Compose. The simulator also uses `Realtime__HubUrl=http://api:8080/hubs/equipment` to publish SignalR updates to the API container.

## Docker Compose

Start the full stack:

```powershell
docker compose up --build
```

Open the operations console:

```text
http://localhost:3000
```

API Swagger is enabled in the Compose profile because the API runs with `ASPNETCORE_ENVIRONMENT=Development` for Phase 1 local deployment:

```text
http://localhost:5000/swagger
```

Stop the stack:

```powershell
docker compose down
```

Remove the local database volume:

```powershell
docker compose down -v
```

## Database Migration

During local Compose startup, the API runs EF Core migrations and seed data when:

- `ASPNETCORE_ENVIRONMENT=Development`
- `ConnectionStrings__MesDatabase` is configured
- the connection string does not contain `CHANGE_ME`

For non-Docker local development, use:

```powershell
dotnet ef database update --project src/MesCopilot.Infrastructure --startup-project src/MesCopilot.Api
```

## Local Development

Backend:

```powershell
dotnet restore
dotnet build .\MesCopilot.sln
dotnet test .\MesCopilot.sln
dotnet run --project .\src\MesCopilot.Api
```

Frontend:

```powershell
cd web
pnpm install
pnpm dev
```

Device simulator:

```powershell
dotnet run --project .\src\MesCopilot.DeviceSimulator
```

## Troubleshooting

| Symptom | Check |
| --- | --- |
| API cannot connect to database | Confirm `postgres` is healthy and `ConnectionStrings__MesDatabase` points to host `postgres` inside Compose |
| Web cannot call API from browser | Set `NEXT_PUBLIC_API_URL=http://localhost:5000/api`, not the internal Compose host |
| Simulator cannot publish equipment updates | Confirm the API container is running and `Realtime__HubUrl` is `http://api:8080/hubs/equipment` |
| Migrations did not run | Confirm the API environment is `Development` and the database password is not `CHANGE_ME` |
| Port is already in use | Override `POSTGRES_PORT`, `API_PORT`, or `WEB_PORT` in `.env` |

## Production Notes

The checked-in Compose file is a Phase 1 local/staging deployment baseline. Before production use:

- Replace all default passwords with environment-specific secrets.
- Run the API with a production environment and an explicit migration process.
- Add TLS termination and CORS policy for the deployed frontend origin.
- Configure persistent backup and restore for PostgreSQL.
- Add centralized logs, metrics, and alerting for API latency and simulator health.

