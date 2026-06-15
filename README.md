# Pigment Interface API

A standalone ASP.NET Core 8 Web API that extracts HR payroll data from the ResourceLink SQL Server middleware database and exposes it to the **Pigment** planning system.

## Solution Structure

```
PigmentInterface/
├── Pigment.API.sln
├── src/
│   ├── Pigment.Core/            # Domain models, interfaces, core service logic
│   ├── Pigment.Infrastructure/  # Dapper SQL repository + Azure Blob Storage service
│   └── Pigment.API/             # ASP.NET Core Web API host (controllers, middleware, DI)
├── sql/
│   └── usp_GetHRDataForPigment.sql   # SQL Server stored procedure (Oracle→SQL Server)
└── tests/
    └── Pigment.API.Tests/       # xUnit unit tests
```

## Endpoints

| Method | Route | Description |
|--------|-------|-------------|
| GET | `/api/v1/hr/json?taxYear=2025&taxPeriod=04` | Returns HR records as JSON |
| GET | `/api/v1/hr/file?taxYear=2025&taxPeriod=04` | Generates pipe-delimited file, uploads to Azure Blob, returns URL |

## Authentication

All endpoints are secured with **Azure Entra ID** (formerly Azure AD) using Bearer token authentication (`Microsoft.Identity.Web`).

## Quick Start

1. Set the following in `appsettings.Development.json` or user-secrets:
   - `AzureAd:TenantId`
   - `AzureAd:ClientId`
   - `ConnectionStrings:MiddlewareDb`
   - `AzureStorage:ConnectionString`
2. Run the stored procedure in `sql/usp_GetHRDataForPigment.sql` against your Middleware database.
3. `dotnet run --project src/Pigment.API`
4. Open `https://localhost:5001/swagger`

## Technology Stack

- .NET 8 / ASP.NET Core Web API
- Dapper (lightweight SQL)
- Azure Blob Storage SDK
- Microsoft.Identity.Web (Entra ID auth)
- Swashbuckle (Swagger/OpenAPI)
- xUnit + Moq (unit tests)
