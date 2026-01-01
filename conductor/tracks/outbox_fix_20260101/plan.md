# Implementation Plan - Pragmatic Outbox for ReceiptsController

## Phase 1: Infrastructure & Data Model
- [ ] Task: Create `OutboxMessage` entity in `Receipts.Infrastructure`.
    - [ ] Sub-task: Define `OutboxMessage` class with properties: `Id`, `OccurredOn`, `Type`, `Payload`, `ProcessedDate`, `Error`.
    - [ ] Sub-task: Add `DbSet<OutboxMessage>` to `ReceiptsDbContext`.
    - [ ] Sub-task: Configure Entity Framework mapping for `OutboxMessage` (indexes, required fields).
- [ ] Task: Create and Apply Migration.
    - [ ] Sub-task: Run `dotnet ef migrations add AddOutboxTable --project Receipts.Infrastructure --startup-project Receipts.API`.
    - [ ] Sub-task: Update local database.

## Phase 2: Transactional Logic Implementation
- [ ] Task: Update `ReceiptsController` to use Outbox.
    - [ ] Sub-task: Write integration test reproducing the dual-write risk (optional but recommended).
    - [ ] Sub-task: Refactor `ProcessReceipt` to wrap database operations in a transaction.
    - [ ] Sub-task: Instead of direct `Enqueue`, create an `OutboxMessage` record with type `ReceiptCreated` and the receipt ID as payload.
    - [ ] Sub-task: Save `Receipt` and `OutboxMessage` in the same transaction.

## Phase 3: Outbox Processor
- [ ] Task: Create Outbox Processor Service.
    - [ ] Sub-task: Implement `IHostedService` or a Hangfire Recurring Job to poll `OutboxMessage` table.
    - [ ] Sub-task: Logic to query unprocessed messages (`ProcessedDate` is null).
    - [ ] Sub-task: Iterate messages and enqueue the actual `IReceiptProcessor` job using `backgroundJobClient`.
    - [ ] Sub-task: Update `OutboxMessage` as processed (set `ProcessedDate`) upon success.
    - [ ] Sub-task: Handle exceptions and update `Error` column in `OutboxMessage`.
- [ ] Task: Register Processor in `Receipts.Worker` or `Receipts.API`.
    - [ ] Sub-task: Register the service in `Program.cs`.

## Phase 4: Verification
- [ ] Task: Verify End-to-End Flow.
    - [ ] Sub-task: Start API and Worker.
    - [ ] Sub-task: Post a new receipt.
    - [ ] Sub-task: Verify `Receipt` and `OutboxMessage` are created.
    - [ ] Sub-task: Verify the Processor picks up the message.
    - [ ] Sub-task: Verify the Hangfire job is enqueued and executed.
    - [ ] Sub-task: Verify `OutboxMessage` is marked as processed.
