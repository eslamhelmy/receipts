using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using NSubstitute;
using Receipts.API.Controllers;
using Receipts.API.Contracts;
using Receipts.API.Services;
using Receipts.Infrastructure;
using Hangfire;
using Hangfire.Common;
using Hangfire.States;
using System.Text.Json;

namespace Receipts.API.Tests;

public class ReceiptsControllerTests
{
    private readonly ReceiptsDbContext _dbContext;
    private readonly IReceiptFileService _receiptFileService;
    private readonly ReceiptsController _controller;

    public ReceiptsControllerTests()
    {
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        _dbContext = new ReceiptsDbContext(options);

        _receiptFileService = Substitute.For<IReceiptFileService>();

        _controller = new ReceiptsController(_dbContext, _receiptFileService);
    }

    [Fact]
    public async Task ProcessReceipt_ShouldCreateOutboxMessage_WhenSuccessful()
    {
        // Arrange
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

        // Act
        var result = await _controller.ProcessReceipt(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        var json = JsonSerializer.Serialize(acceptedResult.Value);
        using var doc = JsonDocument.Parse(json);
        Guid receiptId = doc.RootElement.GetProperty("receiptId").GetGuid();
        
        var outboxMessage = await _dbContext.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(outboxMessage);
        Assert.Contains(receiptId.ToString(), outboxMessage.Payload);
        Assert.Equal("ReceiptCreated", outboxMessage.Type);
        Assert.Null(outboxMessage.ProcessedDate);
    }
}
