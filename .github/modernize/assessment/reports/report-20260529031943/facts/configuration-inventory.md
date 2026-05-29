# Configuration & Externalized Settings Inventory

The project has a compact configuration model centered on `web.config`, environment-variable overrides for DB connectivity, and build/runtime settings in project/container files. No external config server or secret vault integration is declared.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| ASP.NET/WCF config | XML config file | `web.config` | Connection string, WCF service model, health handler registration |
| Build definition | MSBuild project | `ZavaStatementService.csproj` | Target framework, references, compile/content items |
| Package manifest | NuGet package file | `packages.config` | Present but empty |
| Container runtime config | Dockerfile | `Dockerfile` | Mono runtime, compile command, exposed port 8080 |
| Environment variables | External process env | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` and `DATABASE_*` fallbacks | Overrides DB connection string resolution |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| `Debug` | Default when no configuration is supplied | Developer-oriented build output (`bin\Debug`) | Standard .NET Framework references |
| `Release` | Manual/CI build config selection | Production-oriented build output (`bin\Release`) | Standard .NET Framework references |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | App host loads ASP.NET config | `web.config` | Uses `ZavaBankDb` connection string if env vars absent |
| Environment override mode | Process/container environment variables | Environment + `web.config` fallback | Replaces host/port/db/user/password for SQL connectivity |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `connectionStrings:ZavaBankDb` | `Server=sqlserver,1433;Database=ZavaBankDB;User Id=sa;******;TrustServerCertificate=true;` | Default | `web.config` |
| `system.web/compilation@debug` | `true` | Default | `web.config` |
| `system.web/compilation@targetFramework` | `4.8` | Default | `web.config` |
| `system.web/httpRuntime@targetFramework` | `4.8` | Default | `web.config` |
| `DB_HOST` / `DATABASE_HOST` | none | Env override mode | Process environment |
| `DB_PORT` / `DATABASE_PORT` | none | Env override mode | Process environment |
| `DB_NAME` / `DATABASE_NAME` | none | Env override mode | Process environment |
| `DB_USER` / `DATABASE_USER` | none | Env override mode | Process environment |
| `DB_PASSWORD` / `DATABASE_PASSWORD` | none | Env override mode | Process environment |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| `ZavaStatementService` | Mono `xsp4 --port 8080 --address 0.0.0.0 --nonstop` (Docker) | Not explicitly configured | Not explicitly configured |

## Startup Dependency Chain

1. `ZavaStatementService` starts host process.
2. Service requires SQL Server to be reachable before DB-backed operations succeed.
3. Health endpoint can respond independently, but statement operations depend on DB connectivity.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `connectionStrings:ZavaBankDb` password segment | Database credential | `web.config` (masked in docs) |
| `DB_PASSWORD` / `DATABASE_PASSWORD` | Database credential | Environment variable |
| `DB_USER` / `DATABASE_USER` | Database identity | Environment variable |

### Secrets Provisioning Workflow

Secrets can be supplied via deployment environment variables (`DB_*` or fallback `DATABASE_*`) and are consumed at runtime by `DbConfig`. If environment variables are not provided, the service falls back to a static credential in `web.config`, so secure deployment should externalize and rotate DB credentials outside source control.

## Feature Flags

| Flag Name | Default | Controlled By |
|---|---|---|
| None detected | N/A | N/A |

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | 4.8 | `ZavaStatementService.csproj` |
| WCF/ASP.NET runtime APIs | .NET Framework inbox assemblies | `ZavaStatementService.csproj` references |
| Container base image | `mono:6.12` | `Dockerfile` |
| Build tooling format | MSBuild `ToolsVersion=4.0` project format | `ZavaStatementService.csproj` |
