# Receipts Project

## Project Overview

This is a .NET solution designed for processing receipt documents. It implements an asynchronous processing workflow where users upload receipts via an API, and a background worker processes them (simulating OCR).

### Architecture

The solution follows a distributed architecture with the following components:

*   **Receipts.API**: An ASP.NET Core Web API that acts as the entry point.
    *   Accepts receipt uploads (metadata + file).
    *   Validates files (size, type).
    *   Persists initial receipt record to SQL Server with `Pending` status.
    *   Enqueues a background job using **Hangfire**.
*   **Receipts.Worker**: A background worker service (Console App / Worker Service).
    *   Host a Hangfire Server.
    *   Dequeues and processes receipt jobs.
    *   Simulates long-running OCR tasks (5-second delay).
    *   Updates receipt status to `Processed`.
*   **Receipts.Infrastructure**: A shared class library.
    *   Contains the Domain entities (e.g., `Receipt`).
    *   Entity Framework Core `DbContext` configuration.
    *   Migrations.
*   **External Dependencies**:
    *   **SQL Server**: Used for both the application data (Receipts table) and Hangfire job storage. Managed via Docker Compose.

## Key Technologies

*   **Framework**: .NET 8 (or compatible)
*   **Web API**: ASP.NET Core
*   **ORM**: Entity Framework Core
*   **Background Jobs**: Hangfire
*   **Database**: Microsoft SQL Server
*   **Containerization**: Docker Compose

## Building and Running

### Prerequisites

*   .NET SDK (Version 8.0+)
*   Docker Desktop (for SQL Server)

### 1. Start Infrastructure

Start the SQL Server container:

```bash
docker-compose up -d
```

This starts a SQL Server instance on port `1433` with the password `Test1234`.

### 2. Apply Database Migrations

Update the database schema:

```bash
dotnet ef database update --project Receipts.Infrastructure --startup-project Receipts.API
```

### 3. Run the API

Start the API project:

```bash
dotnet run --project Receipts.API
```

The API will be available (typically at `http://localhost:5000` or similar, check console output). Swagger UI is available at `/swagger` in development.

### 4. Run the Worker

Start the background worker to process queued jobs:

```bash
dotnet run --project Receipts.Worker
```

The worker will listen for jobs enqueued by the API.

## Development Conventions

*   **Domain Model**: Entities are defined in `Receipts.Infrastructure`.
*   **Services**: Logic interfacing with external concerns (like file storage) is encapsulated in Services (e.g., `ReceiptFileService`).
*   **Processing Logic**: The core background logic is in `Receipts.Worker/Processors`.
*   **Job Queue**: Hangfire is used for reliable background processing. The API enqueues, and the Worker processes.
*   **Validation**: File validation (size, type) is handled explicitly in `ReceiptFileService`.

## API Usage

**Endpoint**: `POST /api/receipts`

**Content-Type**: `multipart/form-data`

**Parameters**:
*   `File`: (File) The receipt image/PDF (max 1MB).
*   `UserId`: (Guid) ID of the user submitting the receipt.
*   `Amount`: (Decimal) Total amount.
*   `Currency`: (String) Currency code (e.g., USD).
*   `ReceiptDate`: (Date) Date of the receipt (YYYY-MM-DD).

**Response**:
*   `202 Accepted`: Returns `{ "receiptId": "..." }` indicating the receipt is queued for processing.
