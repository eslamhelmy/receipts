using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Receipts.Infrastructure;
using Receipts.Worker.Processors;

namespace Receipts.Worker.Tests;

public class OutboxProcessorTests
{
    private readonly ReceiptsDbContext _dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly ILogger<OutboxProcessor> _logger;
    private readonly OutboxProcessor _processor;

    public OutboxProcessorTests()
    {
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _dbContext = new ReceiptsDbContext(options);

        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _logger = Substitute.For<ILogger<OutboxProcessor>>();

        _processor = new OutboxProcessor(_dbContext, _backgroundJobClient, _logger);
    }

    [Fact]
    public async Task ProcessMessagesAsync_ShouldEnqueueJobAndMarkAsProcessed()
    {
        // Arrange
        var receiptId = Guid.NewGuid();
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            Type = "ReceiptCreated",
            Payload = JsonSerializer.Serialize(new { ReceiptId = receiptId }),
            ProcessedDate = null
        };
        await _dbContext.OutboxMessages.AddAsync(outboxMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessMessagesAsync();

        // Assert
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job => job.Method.Name == "ProcessReceipt" && (Guid)job.Args[0] == receiptId),
            Arg.Any<EnqueuedState>());

        var updatedMessage = await _dbContext.OutboxMessages.FindAsync(outboxMessage.Id);
        Assert.NotNull(updatedMessage!.ProcessedDate);
    }

    [Fact]
    public async Task ProcessMessagesAsync_ShouldIgnoreProcessedMessages()
    {
        // Arrange
        var outboxMessage = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            OccurredOn = DateTime.UtcNow,
            Type = "ReceiptCreated",
            Payload = JsonSerializer.Serialize(new { ReceiptId = Guid.NewGuid() }),
            ProcessedDate = DateTime.UtcNow
        };
        await _dbContext.OutboxMessages.AddAsync(outboxMessage);
        await _dbContext.SaveChangesAsync();

        // Act
        await _processor.ProcessMessagesAsync();

        // Assert
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(Arg.Any<Job>(), Arg.Any<IState>());
    }
}
