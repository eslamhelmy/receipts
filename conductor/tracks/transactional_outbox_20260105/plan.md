# Implementation Plan - Pragmatic Transactional Outbox

## Phase 1: Infrastructure & Data Model [checkpoint: abd5c2d]
- [x] Task: Create `OutboxMessage` entity in `Receipts.Infrastructure`. [21e3166]
    - [ ] Sub-task: Define `OutboxMessage` class with properties: `Id` (Guid), `OccurredOn` (DateTime), `Type` (string), `Payload` (string/JSON), `ProcessedDate` (DateTime?).
    - [ ] Sub-task: Configure the entity mapping in `ReceiptsDbContext` (e.g., indexes on `ProcessedDate`).
- [x] Task: Database Migration. [8e94d61]
    - [ ] Sub-task: Generate a new EF Core migration for the `OutboxMessages` table.
    - [ ] Sub-task: Apply the migration to the local database.
- [x] Task: Conductor - User Manual Verification 'Phase 1: Infrastructure & Data Model' (Protocol in workflow.md)

## Phase 2: Transactional Logic in API
- [x] Task: Update `ReceiptsController` to implement Outbox pattern. [6192eb8]
    - [ ] Sub-task: Write unit tests in `Receipts.API.Tests` to verify that `ReceiptsController` no longer calls `IBackgroundJobClient` directly.
    - [ ] Sub-task: Refactor the POST endpoint to wrap `Receipt` creation and `OutboxMessage` creation in a single `IDbContextTransaction`.
    - [ ] Sub-task: Ensure the `OutboxMessage` payload contains the necessary ID for processing.
- [ ] Task: Conductor - User Manual Verification 'Phase 2: Transactional Logic in API' (Protocol in workflow.md)

## Phase 3: Outbox Processor Job
- [ ] Task: Implement the Outbox Processor logic.
    - [ ] Sub-task: Create a new service or class `OutboxProcessor` to handle the polling logic.
    - [ ] Sub-task: Write unit tests to verify that it correctly identifies unprocessed messages and marks them as processed after "dispatching".
    - [ ] Sub-task: Implement logic to query `OutboxMessages` where `ProcessedDate` is null, and for each, enqueue the actual processing job via Hangfire.
- [ ] Task: Register the Outbox Processor as a Hangfire Recurring Job.
    - [ ] Sub-task: Update `Program.cs` (in API or Worker) to schedule the recurring job (e.g., every minute).
- [ ] Task: Conductor - User Manual Verification 'Phase 3: Outbox Processor Job' (Protocol in workflow.md)

## Phase 4: Final Verification
- [ ] Task: End-to-End Functional Test.
    - [ ] Sub-task: Submit a receipt via the API.
    - [ ] Sub-task: Verify the `OutboxMessage` is created and initially has a null `ProcessedDate`.
    - [ ] Sub-task: Wait for the recurring job to run and verify the message is marked as processed and the receipt is eventually processed by the original worker.
- [ ] Task: Conductor - User Manual Verification 'Phase 4: Final Verification' (Protocol in workflow.md)
