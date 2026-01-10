# Plan: Transactional Outbox Pattern

Implementation of the Transactional Outbox pattern to ensure reliable asynchronous processing of receipts.

## Phase 1: Infrastructure & Data Model [checkpoint: e148af2]
- [x] Task: Create `OutboxMessage` entity in `Receipts.Infrastructure` 14fa465
    - [x] Sub-task: Write Tests for `OutboxMessage` entity validation and serialization
    - [x] Sub-task: Implement `OutboxMessage` entity and `OutboxStatus` enum
- [x] Task: Update `ReceiptsDbContext` to include `OutboxMessages` DbSet 4d7a334
    - [x] Sub-task: Write Integration Tests for `OutboxMessages` persistence
    - [x] Sub-task: Update `ReceiptsDbContext` and create EF Migration
- [x] Task: Conductor - User Manual Verification 'Infrastructure & Data Model' (Protocol in workflow.md)

## Phase 2: Transactional Persistence [checkpoint: 49884a2]
- [x] Task: Implement Outbox Pattern in `ReceiptsController` 738122b
    - [x] Sub-task: Write Tests for `ProcessReceipt` ensuring atomic save of Receipt and OutboxMessage
    - [x] Sub-task: Update `ReceiptsController` to use a transaction and save `OutboxMessage` instead of direct Hangfire enqueue
- [x] Task: Conductor - User Manual Verification 'Transactional Persistence' (Protocol in workflow.md)

## Phase 3: Outbox Dispatcher
- [x] Task: Create `IOutboxDispatcher` and implementation 957a2b0
    - [x] Sub-task: Write Tests for dispatcher polling and job enqueuing logic
    - [x] Sub-task: Implement `OutboxDispatcher` that reads `New` messages and enqueues to Hangfire
- [x] Task: Configure Hangfire Recurring Job for Dispatcher 395a689
    - [x] Sub-task: Write Integration Tests for recurring job registration and execution
    - [x] Sub-task: Register `OutboxDispatcher.Dispatch` as a recurring job in `Program.cs`
- [x] Task: Conductor - User Manual Verification 'Outbox Dispatcher' (Protocol in workflow.md)

## Phase 4: Cleanup & Error Handling
- [ ] Task: Implement retry logic and error state handling in Dispatcher
    - [ ] Sub-task: Write Tests for failed dispatch scenarios and status updates
    - [ ] Sub-task: Update `OutboxDispatcher` with try-catch and status transitions to `Failed`
- [ ] Task: Conductor - User Manual Verification 'Cleanup & Error Handling' (Protocol in workflow.md)
