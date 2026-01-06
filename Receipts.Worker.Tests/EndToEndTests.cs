using System.Text.Json;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Receipts.API.Controllers;
using Receipts.API.Contracts;
using Receipts.API.Services;
using Receipts.Infrastructure;
using Receipts.Worker.Processors;

namespace Receipts.Worker.Tests;

public class EndToEndTests
{
    private readonly ReceiptsDbContext _dbContext;
    private readonly IBackgroundJobClient _backgroundJobClient;
    private readonly IReceiptFileService _receiptFileService;
    private readonly ILogger<OutboxProcessor> _outboxLogger;
    private readonly ReceiptsController _controller;
    private readonly OutboxProcessor _outboxProcessor;

    public EndToEndTests()
    {
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ReceiptsDbContext(options);

        _backgroundJobClient = Substitute.For<IBackgroundJobClient>();
        _receiptFileService = Substitute.For<IReceiptFileService>();
        _outboxLogger = Substitute.For<ILogger<OutboxProcessor>>();

        _controller = new ReceiptsController(_dbContext, _receiptFileService);
        _outboxProcessor = new OutboxProcessor(_dbContext, _backgroundJobClient, _outboxLogger);
    }

    [Fact]
    public async Task FullOutboxFlow_ShouldWorkCorrectly()
    {
        // 1. Arrange: Prepare the request
        var file = Substitute.For<IFormFile>();
        string? error = null;
        _receiptFileService.TryValidate(file, out Arg.Any<string?>()).Returns(x => 
        {
            x[1] = error;
            return true;
        });
        _receiptFileService.SimulateUploadAsync(file).Returns("http://example.com/file.pdf");

        var request = new CreateReceiptRequest
        {
            UserId = Guid.NewGuid(),
            Amount = 100.00m,
            Currency = "USD",
            ReceiptDate = DateOnly.FromDateTime(DateTime.UtcNow),
            File = file
        };

        // 2. Act: Call the controller (Step 1 of Outbox)
        var result = await _controller.ProcessReceipt(request);
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var json = JsonSerializer.Serialize(acceptedResult.Value);
        using var doc = JsonDocument.Parse(json);
        Guid receiptId = doc.RootElement.GetProperty("receiptId").GetGuid();

        // 3. Assert: Verify OutboxMessage is created but not processed
        var message = await _dbContext.OutboxMessages.SingleAsync();
        Assert.Null(message.ProcessedDate);
        _backgroundJobClient.DidNotReceiveWithAnyArgs().Create(Arg.Any<Job>(), Arg.Any<IState>());

        // 4. Act: Run the outbox processor (Step 2 of Outbox)
        await _outboxProcessor.ProcessMessagesAsync();

        // 5. Assert: Verify job is enqueued and message is marked as processed
        _backgroundJobClient.Received(1).Create(
            Arg.Is<Job>(job => job.Method.Name == "ProcessReceipt" && (Guid)job.Args[0] == receiptId),
            Arg.Any<EnqueuedState>());

        var updatedMessage = await _dbContext.OutboxMessages.SingleAsync();
        Assert.NotNull(updatedMessage.ProcessedDate);
    }
}
