# Dependency Map

This project has a very small declared dependency surface: it relies almost entirely on .NET Framework assemblies plus the Mono runtime used by the Docker image. No NuGet packages are declared in `packages.config`.

## Dependencies

```mermaid
flowchart LR
    App["ZavaStatementService"]

    subgraph Web["Web Frameworks"]
        AspNet["System.Web .NET Framework 4.8"]
        Wcf["System.ServiceModel .NET Framework 4.8"]
    end
    subgraph DB["Database / ORM"]
        AdoNet["System.Data .NET Framework 4.8"]
        SqlClient["SqlClient provider from framework config"]
    end
    subgraph Sec["Security"]
        Security["System.Security .NET Framework 4.8"]
    end
    subgraph Util["Utilities"]
        Config["System.Configuration .NET Framework 4.8"]
        Serialization["System.Runtime.Serialization .NET Framework 4.8"]
        Core["System and System.Core .NET Framework 4.8"]
        Mono["mono 6.12 and mono-xsp4"]
    end

    App -->|"web"| Web
    App -->|"persistence"| DB
    App -->|"security"| Sec
    App -->|"runtime"| Util
    Mono -.->|"hosts framework assemblies"| AspNet
    Mono -.->|"hosts framework assemblies"| Wcf
```

### Dependency Summary

| Category | Count | Key Libraries | Notes |
|---|---|---|---|
| Web Frameworks | 2 | System.Web, System.ServiceModel | Legacy ASP.NET and WCF service stack on .NET Framework 4.8 |
| Database / ORM | 2 | System.Data, SqlClient provider | Uses raw ADO.NET and a SQL Server connection string |
| Security | 1 | System.Security | Used for HTML escaping and checksum support |
| Utilities | 4 | System.Configuration, System.Runtime.Serialization, System, Mono 6.12 | Core runtime and serialization support with Mono container hosting |

### Version & Compatibility Risks

The application targets .NET Framework 4.8 and WCF server hosting, both of which are legacy workloads for Linux container modernization. The Docker image depends on Mono 6.12 and `mono-xsp4`, which helps runtime compatibility today but increases migration effort when moving to modern .NET hosting models.

### Notable Observations

- `packages.config` is empty, so modernization risk comes primarily from framework/runtime dependencies rather than third-party packages.
- `System.ServiceModel` indicates a server-side WCF contract that does not have a direct one-step migration path to modern .NET.
- Database access is performed with inline SQL and framework assemblies only, so no ORM package upgrade path is present.
- The Dockerfile builds with `mcs` and hosts with `xsp4`, reinforcing the dependency on the Mono runtime rather than the .NET SDK runtime.

## Test Dependencies

No test-scoped dependencies detected.

Total test-scope dependencies: 0

No automated test framework is declared in build or package files.
