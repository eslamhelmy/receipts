# Technology Stack: Receipts Management System

## Backend
- **Framework:** .NET 10 (C#) / ASP.NET Core
  - Provides a high-performance, cross-platform foundation for the API and worker services.
- **Background Jobs:** Hangfire
  - Manages asynchronous processing for OCR tasks and scheduled reconciliation jobs, ensuring reliability and observability.
- **Outbox Pattern:**
  - Implemented to guarantee atomic persistence of business data and background job intent.

## Data Persistence
- **Database:** SQL Server
  - A robust relational database for storing receipt metadata, reconciliation records, and audit logs.
- **ORM:** Entity Framework Core
  - Handles data access and migrations, providing a strongly-typed interface to the database.

## Infrastructure & Deployment
- **Containerization:** Docker (Database Only)
  - Docker is used exclusively to host the SQL Server instance for development. The API and Worker services are run locally on the host machine.
- **Architecture:** Layered Solution
  - Separates concerns into `Receipts.API` (interface), `Receipts.Worker` (processing), and `Receipts.Infrastructure` (data and shared logic).
