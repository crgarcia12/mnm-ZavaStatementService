# Configuration & Externalized Settings Inventory

The configuration surface is small and centralized around `web.config`, environment-variable database overrides, and the Docker runtime command. No external configuration server, secret manager, or feature flag system is present.

## Configuration Sources

| Source | Type | Path/Location | Notes |
|---|---|---|---|
| `web.config` | ASP.NET application config | `/tmp/workspace/crgarcia12/mnm-ZavaStatementService/web.config` | Defines SQL Server connection string, WCF service host settings, and `/health` authorization |
| Environment variables | Runtime overrides | Process environment | `DB_HOST`, `DB_PORT`, `DB_NAME`, `DB_USER`, `DB_PASSWORD` and `DATABASE_*` fallbacks override database connectivity |
| `Dockerfile` | Container runtime config | `/tmp/workspace/crgarcia12/mnm-ZavaStatementService/Dockerfile` | Selects `mono:6.12`, installs `mono-xsp4`, exposes port 8080, and defines the runtime command |
| `ZavaStatementService.csproj` | Build config | `/tmp/workspace/crgarcia12/mnm-ZavaStatementService/ZavaStatementService.csproj` | Defines .NET Framework 4.8 target and debug or release output paths |
| `packages.config` | Package manifest | `/tmp/workspace/crgarcia12/mnm-ZavaStatementService/packages.config` | Present but empty |

## Build Profiles

| Profile | Activation | Purpose | Key Dependencies/Plugins |
|---|---|---|---|
| Debug | Default when no configuration is passed | Writes binaries to `bin\Debug\` | Uses the project’s .NET Framework 4.8 reference set |
| Release | Manual build configuration selection | Writes binaries to `bin\Release\` | Uses the same framework references with release output path |

## Runtime Profiles

| Profile | Activation Method | Config Files | Key Overrides |
|---|---|---|---|
| Default | No environment overrides present | `web.config` | Uses embedded `ZavaBankDb` SQL Server connection string and WCF hosting configuration |
| Externalized DB runtime | Set `DB_*` or `DATABASE_*` environment variables | `web.config` plus environment variables | Environment variables replace host, port, database name, username, and password |

## Properties Inventory

| Property Key | Default | Profiles | Source |
|---|---|---|---|
| `connectionStrings.ZavaBankDb` | `Server=sqlserver,1433;Database=ZavaBankDB;User Id=sa;******;TrustServerCertificate=true;` | Default | `web.config` |
| `system.web.compilation.debug` | `true` | Default | `web.config` |
| `system.web.compilation.targetFramework` | `4.8` | Default | `web.config` |
| `system.web.httpRuntime.targetFramework` | `4.8` | Default | `web.config` |
| `system.web.customErrors.mode` | `Off` | Default | `web.config` |
| `system.serviceModel.serviceMetadata.httpGetEnabled` | `true` | Default | `web.config` |
| `DB_HOST` or `DATABASE_HOST` | unset | Externalized DB runtime | Environment |
| `DB_PORT` or `DATABASE_PORT` | unset | Externalized DB runtime | Environment |
| `DB_NAME` or `DATABASE_NAME` | unset | Externalized DB runtime | Environment |
| `DB_USER` or `DATABASE_USER` | unset | Externalized DB runtime | Environment |
| `DB_PASSWORD` or `DATABASE_PASSWORD` | unset | Externalized DB runtime | Environment |

## Startup Parameters & Resource Requirements

| Service | JVM/Runtime Options | Memory | Instance Count |
|---|---|---|---|
| ZavaStatementService | `xsp4 --port 8080 --address 0.0.0.0 --nonstop` | Not specified | Not specified |

## Startup Dependency Chain

1. `mono-xsp4` starts the ASP.NET/WCF application on port 8080.
2. `ZavaStatementService` becomes useful only after SQL Server is reachable with the configured connection details.
3. The `/health` handler can respond without database access, but statement operations will fail until SQL Server is available.

## Secrets & Sensitive Configuration

| Secret Reference | Type | Storage (masked) |
|---|---|---|
| `connectionStrings.ZavaBankDb` password | SQL credential | `web.config` with password masked in this document |
| `DB_PASSWORD` or `DATABASE_PASSWORD` | SQL credential | Environment variable |
| `DB_USER` or `DATABASE_USER` | SQL username | Environment variable |

### Secrets Provisioning Workflow

Secrets are provisioned either statically in `web.config` or dynamically through environment variables read by `DbConfig`. No managed identity, Key Vault, Vault, or deployment-time secret synchronization flow is defined in the repository, so secret distribution appears to rely on whoever supplies the process environment or configuration file.

## Feature Flags

No feature flags or conditional runtime toggles were detected.

## Framework & Runtime Versions

| Component | Version | Source |
|---|---|---|
| .NET Framework target | 4.8 | `ZavaStatementService.csproj` |
| MSBuild project tools version | 4.0 | `ZavaStatementService.csproj` |
| ASP.NET and WCF runtime assemblies | .NET Framework 4.8 | `ZavaStatementService.csproj` references |
| Mono base image | 6.12 | `Dockerfile` |
| Host server | `mono-xsp4` | `Dockerfile` |
