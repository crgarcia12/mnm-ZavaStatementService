# Architecture Diagram

This repository contains a single ASP.NET/WCF statement-generation service. The application exposes SOAP operations for statement generation and retrieval, serves a lightweight landing page and health probe, and reads and writes statement data in a shared SQL Server database.

## Application Architecture

```mermaid
flowchart TD
    subgraph Client["Client Layer"]
        Browser["Browser or SOAP Client"]
    end
    subgraph Presentation["Presentation Layer - ASP.NET and WCF"]
        Landing["Default.aspx status page"]
        Endpoint["ZavaStatementService.svc basicHttpBinding"]
        Health["HealthHandler GET /health"]
    end
    subgraph Business["Business Logic - ZavaStatementService"]
        Service["StatementService"]
        Contracts["IStatementService and DTO contracts"]
    end
    subgraph Data["Data Layer - SQL Server"]
        Config["DbConfig connection resolution"]
        CoreTables[("Accounts Customers Transactions TransactionTypes")]
        StatementTables[("StatementRequests GeneratedStatements StatementArchive")]
    end
    subgraph Runtime["Runtime"]
        Mono["Mono 6.12 with xsp4"]
    end

    Browser -->|"browse status page"| Landing
    Browser -->|"SOAP requests"| Endpoint
    Browser -->|"health probe"| Health
    Endpoint -->|"invoke operations"| Service
    Service -->|"serialize responses"| Contracts
    Service -->|"resolve connection string"| Config
    Config -->|"connect"| CoreTables
    Service -->|"read account and transaction data"| CoreTables
    Service -->|"persist requests and generated HTML"| StatementTables
    Mono -->|"hosts"| Landing
    Mono -->|"hosts"| Endpoint
    Mono -->|"hosts"| Health
```

### Technology Stack Summary

| Layer | Technology | Version | Purpose |
|---|---|---|---|
| Client | Browser or SOAP client | N/A | Calls the WCF service and health endpoint |
| Presentation | ASP.NET Web Forms and WCF basicHttpBinding | .NET Framework 4.8 | Exposes the landing page, SOAP endpoint, and health handler |
| Business | StatementService | Repository code | Validates input, builds HTML statements, and orchestrates database operations |
| Data Access | ADO.NET SqlConnection and SqlCommand | .NET Framework 4.8 | Executes inline SQL against SQL Server |
| Data Storage | SQL Server | Not pinned in repo | Stores account, transaction, request, archive, and generated statement data |
| Runtime | Mono with xsp4 | 6.12 | Hosts the service in the containerized runtime |

### Data Storage & External Services

The only external dependency visible in the repository is SQL Server, configured through either environment variables or the `ZavaBankDb` connection string in `web.config`. The service reads customer, account, and transaction data from shared banking tables and writes statement request, generated statement, and archive records back into the same database.

### Key Architectural Decisions

- Uses a single-service, database-centric design with direct inline SQL instead of repository or ORM abstractions.
- Exposes business operations as SOAP contracts over WCF while keeping health monitoring as a simple HTTP handler.
- Supports both config-file and environment-variable database configuration so the same code can run under IIS-style hosting or containerized Mono hosting.

## Component Relationships

```mermaid
flowchart LR
    subgraph Presentation
        DefaultPage["Default.aspx"]
        SvcHost["ZavaStatementService.svc"]
        HealthHandler["HealthHandler"]
    end
    subgraph Business["Business Logic"]
        StatementSvc["StatementService"]
        Contract["IStatementService"]
        Dtos["StatementContracts DTOs"]
    end
    subgraph DataAccess["Data Access"]
        DbCfg["DbConfig"]
        SqlClient["SqlConnection and SqlCommand"]
        SharedDb["Shared SQL Server tables"]
    end
    subgraph Infra["Infrastructure"]
        WebConfig["web.config"]
        MonoHost["mono xsp4 host"]
    end

    DefaultPage -->|"links to WSDL"| SvcHost
    SvcHost -->|"dispatches to"| Contract
    Contract -->|"implemented by"| StatementSvc
    StatementSvc -->|"returns"| Dtos
    StatementSvc -->|"reads config"| DbCfg
    DbCfg -->|"connection string"| WebConfig
    StatementSvc -->|"opens commands through"| SqlClient
    SqlClient -->|"queries and writes"| SharedDb
    MonoHost -.->|"hosts"| DefaultPage
    MonoHost -.->|"hosts"| SvcHost
    MonoHost -.->|"hosts"| HealthHandler
    HealthHandler -.->|"bypasses business logic"| WebConfig
```

### Component Inventory

| Component | Layer | Type | Responsibility |
|---|---|---|---|
| Default.aspx | Presentation | Web Forms page | Shows service status and links to the WSDL |
| ZavaStatementService.svc | Presentation | WCF service host | Exposes the SOAP endpoint for statement operations |
| HealthHandler | Presentation | HTTP handler | Returns `OK` for readiness and health checks |
| IStatementService | Business Logic | WCF service contract | Declares the three public service operations |
| StatementService | Business Logic | Service class | Validates inputs, queries SQL Server, generates HTML, and stores statement records |
| StatementContracts DTOs | Business Logic | Data contracts | Shapes generated statement, history, and detail responses |
| DbConfig | Data Access | Configuration helper | Resolves DB settings from environment variables or `web.config` |
| SqlConnection and SqlCommand | Data Access | ADO.NET client | Executes inline SQL for read and write operations |
| Shared SQL Server tables | Data Access | Database | Persists core banking data and statement artifacts |
| mono xsp4 host | Infrastructure | Runtime host | Serves the ASP.NET/WCF application in the container image |
