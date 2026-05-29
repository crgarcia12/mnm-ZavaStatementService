# Modernization Plan: Zava Statement Service Azure Modernization

**Project**: Zava Statement Service

---

## Technical Framework

- **Language**: C# (.NET Framework 4.8)
- **Framework**: ASP.NET Web Services (.svc) on .NET Framework
- **Build Tool**: MSBuild (legacy .csproj format)
- **Database**: SQL Server
- **Key Dependencies**: System.Web, System.ServiceModel, System.Data.SqlClient

---

## Overview

This migration modernizes the Zava Statement Service and deploys it to Azure.
The application currently runs on legacy .NET Framework 4.8 with traditional
service hosting and non-cloud-native runtime assumptions. The new architecture
will:

- Upgrade the application to the latest .NET LTS framework baseline
- Adopt Azure cloud-native services for runtime integration and identity
- Deploy the service to Azure Container Apps for managed cloud operations

The migration follows a phased approach of runtime upgrade, service
modernization, security hardening, and Azure deployment.

---

## Migration Impact Summary

| Application | Original Service | New Azure Service | Authentication | Comments |
|-------------|------------------|-------------------|----------------|----------|
| Zava Statement Service | Legacy .NET 4.8 host | Azure Container Apps | Managed Identity | Modernize app and deploy to Azure |
| Zava Statement Service | SQL Server integration | Azure SQL Database | Managed Identity | Move data integration to cloud-native service |

---

## Security Compliance

**Description**: Scan all project dependencies for known CVEs and remediate
any identified vulnerabilities to ensure the application is secure before deployment.

**Requirements**:
Upgrade vulnerable dependencies to the minimum patched version. If a CVE fix
requires a major version upgrade, document the affected dependency, the current
version, the upgraded major version, and the breaking change risk. Verify that
the project builds and all tests pass after remediation.
