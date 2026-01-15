# Specification: Transactional Outbox Pattern

## Overview
Implement the Transactional Outbox pattern to resolve the dual-write problem in the `ReceiptsController`. Currently, the system commits a receipt to the database and then enqueues a Hangfire job. If the enqueue fails, the system enters an inconsistent state. The Outbox pattern ensures that the data change and the message intended for the background worker are saved atomically in the same database transaction.

## Functional Requirements
- **Generic Outbox Entity:** Create a generic `OutboxMessage` entity to store events/messages that need to be processed asynchronously.
- **Atomic Persistence:** Update the `ReceiptsController` to save the `Receipt` and a corresponding `OutboxMessage` within a single database transaction.
- **Polling Publisher (Hangfire):** Implement a recurring Hangfire job that polls the `OutboxMessage` table for unprocessed messages.
- **Message Dispatching:** The dispatcher will deserialize the payload and enqueue the appropriate background task (e.g., `IReceiptProcessor.ProcessReceipt`).
- **Status Management:** Track the status of each outbox message (`New`, `Processing`, `Completed`, `Failed`) and record processing timestamps.

## Technical Details
- **Schema:**
    - `Id`: Guid (Primary Key)
    - `Type`: String (Full name of the message type)
    - `Payload`: String (JSON serialized data)
    - `Status`: Enum (New, Processing, Completed, Failed)
    - `CreatedAt`: DateTime
    - `ProcessedAt`: DateTime (Nullable)
- **Serialization:** Use `System.Text.Json` for payload serialization.
- **Dispatcher:** A dedicated service triggered by a Hangfire recurring job every minute (or configurable interval).

## Acceptance Criteria
- [ ] Submitting a receipt successfully creates both a `Receipt` record and an `OutboxMessage` record in the same transaction.
- [ ] If the database transaction fails, neither the `Receipt` nor the `OutboxMessage` is persisted.
- [ ] The Hangfire recurring job successfully picks up "New" outbox messages.
- [ ] The dispatcher correctly enqueues the `IReceiptProcessor` job and marks the outbox message as `Completed`.
- [ ] Failed dispatch attempts are marked as `Failed` and can be retried (manually or automatically).
