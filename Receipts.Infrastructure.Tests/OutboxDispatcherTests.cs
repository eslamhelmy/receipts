using System;
using System.Linq;
using System.Threading.Tasks;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.EntityFrameworkCore;
using Moq;
using Receipts.Infrastructure;
using Receipts.Infrastructure.Messages;
using Xunit;

namespace Receipts.Infrastructure.Tests;

public class OutboxDispatcherTests
{
    [Fact]
    public async Task DispatchAsync_Should_Process_New_Messages_And_Mark_Completed()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var receiptId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new ProcessReceiptMessage(receiptId)),
            Status = OutboxStatus.New
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        var dispatcher = new OutboxDispatcher(dbContext, backgroundJobClientMock.Object);

        // Act
        await dispatcher.DispatchAsync();

        // Assert
        var updatedMessage = await dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updatedMessage);
        Assert.Equal(OutboxStatus.Completed, updatedMessage.Status);
        Assert.NotNull(updatedMessage.ProcessedAt);

        // Verify Hangfire enqueue was called
        backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == "ProcessReceipt" && (Guid)job.Args[0] == receiptId),
            It.IsAny<IState>()), Times.Once);
    }

    [Fact]
    public async Task DispatchAsync_Should_Ignore_Already_Processed_Messages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var message = new OutboxMessage
        {
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = "{}",
            Status = OutboxStatus.Completed,
            ProcessedAt = DateTime.UtcNow
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        var dispatcher = new OutboxDispatcher(dbContext, backgroundJobClientMock.Object);

        // Act
        await dispatcher.DispatchAsync();

        // Assert
        backgroundJobClientMock.Verify(x => x.Create(
            It.IsAny<Job>(),
            It.IsAny<IState>()), Times.Never);
    }

    [Fact]
    public async Task DispatchAsync_Should_Mark_Failed_On_Exception()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var message = new OutboxMessage
        {
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = "invalid-json", 
            Status = OutboxStatus.New
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        var dispatcher = new OutboxDispatcher(dbContext, backgroundJobClientMock.Object);

        // Act
        await dispatcher.DispatchAsync();

        // Assert
        var updatedMessage = await dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updatedMessage);
        Assert.Equal(OutboxStatus.Failed, updatedMessage.Status);
        Assert.Equal(1, updatedMessage.RetryCount);
        Assert.NotNull(updatedMessage.Error);
    }

    [Fact]
    public async Task DispatchAsync_Should_Retry_Failed_Messages()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var receiptId = Guid.NewGuid();
        var message = new OutboxMessage
        {
            Type = typeof(ProcessReceiptMessage).FullName!,
            Payload = System.Text.Json.JsonSerializer.Serialize(new ProcessReceiptMessage(receiptId)),
            Status = OutboxStatus.Failed,
            RetryCount = 1
        };
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var backgroundJobClientMock = new Mock<IBackgroundJobClient>();
        var dispatcher = new OutboxDispatcher(dbContext, backgroundJobClientMock.Object);

        // Act
        await dispatcher.DispatchAsync();

        // Assert
        var updatedMessage = await dbContext.OutboxMessages.FindAsync(message.Id);
        Assert.NotNull(updatedMessage);
        Assert.Equal(OutboxStatus.Completed, updatedMessage.Status);
        Assert.Equal(1, updatedMessage.RetryCount); // Not incremented if successful

        backgroundJobClientMock.Verify(x => x.Create(
            It.Is<Job>(job => job.Method.Name == "ProcessReceipt" && (Guid)job.Args[0] == receiptId),
            It.IsAny<IState>()), Times.Once);
    }
}
