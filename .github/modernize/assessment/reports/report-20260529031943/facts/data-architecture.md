# Data Architecture & Persistence Layer

The persistence layer is SQL Server-centric and implemented through direct ADO.NET commands rather than an ORM. Core data interactions cover account lookup, transaction retrieval, generated statement persistence, and archive tracking.

## Database Configuration

| Service/Module | DB Type | Profile | Driver | Connection | Migration Tool |
|---|---|---|---|---|---|
| `ZavaStatementService` | SQL Server | Default runtime | `System.Data.SqlClient` | Environment variables (`DB_*`/`DATABASE_*`) or `web.config` connection string (`ZavaBankDb`) | None detected |

## Data Ownership per Service

| Service | Tables Owned | ORM Framework | Caching | Notes |
|---|---|---|---|---|
| `ZavaStatementService` | Reads: `Accounts`, `Customers`, `Transactions`, `TransactionTypes`; Writes: `StatementRequests`, `GeneratedStatements`, `StatementArchive` | Raw ADO.NET (no ORM) | None | Creates `GeneratedStatements` table on demand if missing |

## Entity Model

```mermaid
erDiagram
    ACCOUNTS ||--|| CUSTOMERS : "belongs to customer"
    ACCOUNTS ||--o{ TRANSACTIONS : "has transactions"
    TRANSACTIONS }o--|| TRANSACTIONTYPES : "classified by"
    STATEMENTREQUESTS ||--o{ GENERATEDSTATEMENTS : "produces"
    GENERATEDSTATEMENTS ||--o{ STATEMENTARCHIVE : "archived as"
    ACCOUNTS ||--o{ STATEMENTREQUESTS : "requested for"
    ACCOUNTS ||--o{ GENERATEDSTATEMENTS : "statement for"

    ACCOUNTS {
        int AccountID PK
        int CustomerID FK
        string AccountNumber
        decimal Balance
    }
    CUSTOMERS {
        int CustomerID PK
        string FirstName
        string LastName
    }
    TRANSACTIONS {
        int TransactionID PK
        int AccountID FK
        int TransactionTypeID FK
        date TransactionDate
        decimal Amount
        decimal BalanceAfter
    }
    TRANSACTIONTYPES {
        int TransactionTypeID PK
        string TypeCode
    }
    STATEMENTREQUESTS {
        int RequestID PK
        int CustomerID FK
        int AccountID FK
        date PeriodStart
        date PeriodEnd
        string Status
    }
    GENERATEDSTATEMENTS {
        int StatementID PK
        int RequestID FK
        int AccountID FK
        int StatementMonth
        int StatementYear
        string StatementHtml
    }
    STATEMENTARCHIVE {
        int ArchiveID PK
        int RequestID FK
        int AccountID FK
        string FilePath
        int FileSize
        string Checksum
    }
```

## Key Repository Methods

| Service | Repository | Notable Methods | Purpose |
|---|---|---|---|
| `ZavaStatementService` | `StatementService` (`StatementService.cs`) | `GenerateStatement(int,int,int)` | Reads account+transaction data, computes totals, persists request and generated statement |
| `ZavaStatementService` | `StatementService` (`StatementService.cs`) | `GetStatementHistory(int)` | Returns latest generated statements for an account |
| `ZavaStatementService` | `StatementService` (`StatementService.cs`) | `GetStatement(int)` | Retrieves stored HTML content for a statement ID |
| `ZavaStatementService` | `StatementService` (`StatementService.cs`) | `EnsureGeneratedStatementsTableExists(SqlConnection)` | Creates `GeneratedStatements` table if absent |

## Caching Strategy

No caching provider or cache abstraction is configured. All reads are executed directly against SQL Server, and generated statement HTML is persisted in `GeneratedStatements` for later retrieval.

## Data Ownership Boundaries

This application is a single service with one shared SQL Server database. Data access is direct from the service to database tables with no separate data-service boundary and no CQRS split; the same service handles both read and write paths.

### Data Classification & Sensitivity

| Entity | Sensitive Fields | Classification (PII/PHI/PCI/None) | Controls in Place |
|---|---|---|---|
| `Customers` | `FirstName`, `LastName` | PII | No explicit field-level masking or encryption controls in repository |
| `Accounts` | `AccountNumber` | PII/financial | No explicit field-level masking or encryption controls in repository |
| `StatementArchive` | `FilePath`, `Checksum` | Internal | Integrity checksum stored; no additional confidentiality controls shown |
