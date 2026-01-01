# Specification: Pragmatic Fix for Dual-Write Problem in ReceiptsController

## Overview
This track aims to address the "dual-write" problem in the `ReceiptsController`. Currently, the controller saves receipt data to the database and then immediately enqueues a background job using Hangfire. If the database save succeeds but the job enqueue fails (or vice versa, if the order were reversed), the system enters an inconsistent state. We will implement a pragmatic version of the Outbox Pattern to ensure atomicity and reliability.

## Problem Statement
The `ProcessReceipt` method in `ReceiptsController.cs` performs two non-atomic operations:
1. `await dbContext.SaveChangesAsync();`
2. `backgroundJobClient.Enqueue<IReceiptProcessor>(...);`

If `Enqueue` fails after `SaveChangesAsync` succeeds, the receipt remains in `Pending` status forever, as the background processing is never triggered.

## Proposed Solution (Pragmatic Outbox)
Instead of enqueuing the Hangfire job directly in the controller, we will:
1. Create an `OutboxMessage` table.
2. In a single database transaction, save the `Receipt` and an `OutboxMessage` representing the need to process that receipt.
3. Use a background worker (or a Hangfire recurring job) to poll the `OutboxMessage` table and enqueue the actual processing tasks.

## Functional Requirements
- **Atomic Operations:** Ensure `Receipt` creation and its corresponding processing intent are saved in a single transaction.
- **Reliable Processing:** Implement a mechanism to process pending outbox messages and trigger the `IReceiptProcessor`.
- **Idempotency:** Ensure the processing logic can handle potential duplicate triggers (a requirement of the Outbox pattern).

## Non-Functional Requirements
- **Minimal Architectural Impact:** Avoid introducing new external infrastructure like message brokers.
- **Maintainability:** Follow existing project patterns and .NET 10 conventions.

## Acceptance Criteria
- [ ] A new `OutboxMessage` entity and corresponding database table are created.
- [ ] `ReceiptsController` is updated to save `Receipt` and `OutboxMessage` within a transaction.
- [ ] A background process successfully picks up `OutboxMessage` records and enqueues Hangfire jobs.
- [ ] Processed `OutboxMessage` records are marked as processed or removed.
- [ ] Integration tests verify that receipts are eventually processed even if the initial job enqueue logic were to fail (simulated).

## Out of Scope
- Implementing a full-blown Distributed Transaction Coordinator.
- Introducing a message bus like RabbitMQ or Azure Service Bus.
