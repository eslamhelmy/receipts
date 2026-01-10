# Plan: Transactional Outbox Pattern

Implementation of the Transactional Outbox pattern to ensure reliable asynchronous processing of receipts.

## Phase 1: Infrastructure & Data Model
- [x] Task: Create `OutboxMessage` entity in `Receipts.Infrastructure` 14fa465
    - [x] Sub-task: Write Tests for `OutboxMessage` entity validation and serialization
    - [x] Sub-task: Implement `OutboxMessage` entity and `OutboxStatus` enum
- [ ] Task: Update `ReceiptsDbContext` to include `OutboxMessages` DbSet
    - [ ] Sub-task: Write Integration Tests for `OutboxMessages` persistence
    - [ ] Sub-task: Update `ReceiptsDbContext` and create EF Migration
- [ ] Task: Conductor - User Manual Verification 'Infrastructure & Data Model' (Protocol in workflow.md)

## Phase 2: Transactional Persistence
- [ ] Task: Implement Outbox Pattern in `ReceiptsController`
    - [ ] Sub-task: Write Tests for `ProcessReceipt` ensuring atomic save of Receipt and OutboxMessage
    - [ ] Sub-task: Update `ReceiptsController` to use a transaction and save `OutboxMessage` instead of direct Hangfire enqueue
- [ ] Task: Conductor - User Manual Verification 'Transactional Persistence' (Protocol in workflow.md)

## Phase 3: Outbox Dispatcher
- [ ] Task: Create `IOutboxDispatcher` and implementation
    - [ ] Sub-task: Write Tests for dispatcher polling and job enqueuing logic
    - [ ] Sub-task: Implement `OutboxDispatcher` that reads `New` messages and enqueues to Hangfire
- [ ] Task: Configure Hangfire Recurring Job for Dispatcher
    - [ ] Sub-task: Write Integration Tests for recurring job registration and execution
    - [ ] Sub-task: Register `OutboxDispatcher.Dispatch` as a recurring job in `Program.cs`
- [ ] Task: Conductor - User Manual Verification 'Outbox Dispatcher' (Protocol in workflow.md)

## Phase 4: Cleanup & Error Handling
- [ ] Task: Implement retry logic and error state handling in Dispatcher
    - [ ] Sub-task: Write Tests for failed dispatch scenarios and status updates
    - [ ] Sub-task: Update `OutboxDispatcher` with try-catch and status transitions to `Failed`
- [ ] Task: Conductor - User Manual Verification 'Cleanup & Error Handling' (Protocol in workflow.md)
