# Specification: Pragmatic Transactional Outbox for ReceiptsController

## Overview
Currently, the `ReceiptsController` in `Receipts.API` uses the Hangfire client directly to enqueue background jobs when a receipt is processed. This creates a "dual-write" risk: if the database save succeeds but the Hangfire enqueue fails (or vice versa), the system enters an inconsistent state. This track implements the Transactional Outbox pattern to ensure that the receipt data and the processing intent are saved atomically in a single database transaction.

## Functional Requirements
1.  **Outbox Storage:**
    -   Create an `OutboxMessages` table in the existing SQL Server database (via `ReceiptsDbContext`).
    -   The table should store: `Id`, `OccurredOn`, `Type` (e.g., "ReceiptCreated"), `Payload` (JSON), and `ProcessedDate` (null if pending).
2.  **Atomic Persistence:**
    -   Refactor the `ReceiptsController` (or the underlying service) to save the `Receipt` entity and a corresponding `OutboxMessage` record within a single Entity Framework transaction.
3.  **Outbox Processing:**
    -   Implement a Hangfire Recurring Job that polls the `OutboxMessages` table for unprocessed records (`ProcessedDate IS NULL`).
    -   For each pending message, the job will enqueue the actual receipt processing task using the standard Hangfire client.
    -   Once enqueued successfully, the `OutboxMessage` must be marked as processed (update `ProcessedDate`).

## Non-Functional Requirements
-   **Reliability:** The system must guarantee that every receipt saved to the database eventually triggers a processing job.
-   **Idempotency:** While out-of-scope for this specific track, the downstream processors should ideally handle duplicate messages, as the outbox pattern guarantees "at-least-once" delivery.

## Acceptance Criteria
- [ ] A new `OutboxMessages` table exists in the database.
- [ ] `ReceiptsController` no longer enqueues jobs directly to Hangfire during the HTTP request.
- [ ] A new receipt submission results in both a `Receipt` record and an `OutboxMessage` record in the database.
- [ ] A Hangfire Recurring Job picks up pending `OutboxMessage` records and enqueues the processing job.
- [ ] `OutboxMessage` records are updated with a `ProcessedDate` after the job is enqueued.

## Out of Scope
-   Cleaning up old, processed outbox messages (can be handled in a future chore).
-   Advanced error handling/retries for the outbox processor itself (Hangfire's built-in retry mechanism will be used).
