# Technology Stack

## Core Technologies
- **Language:** C# (targetting .NET 10.0)
- **Runtime:** .NET 10.0

## Backend Services
- **API Framework:** ASP.NET Core (Microsoft.NET.Sdk.Web)
- **Background Worker:** .NET Worker Service (Microsoft.NET.Sdk.Worker)

## Data Management
- **ORM:** Entity Framework Core 10.0
- **Database:** Microsoft SQL Server
- **Migration Tool:** EF Core Migrations

## Task Scheduling & Asynchrony
- **Background Jobs:** Hangfire (using SQL Server storage)
- **Job Orchestration:** Transactional Outbox pattern for reliable message dispatching.
- **Message Serialization:** System.Text.Json

## Documentation & Tooling
- **API Documentation:** Swashbuckle / Swagger UI
- **Coding Helpers:** Ardalis.GuardClauses
