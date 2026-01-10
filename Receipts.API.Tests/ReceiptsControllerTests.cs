using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using Receipts.API.Controllers;
using Receipts.API.Contracts;
using Receipts.API.Services;
using Receipts.Infrastructure;
using Hangfire;
using Xunit;

namespace Receipts.API.Tests;

public class ReceiptsControllerTests
{
    [Fact]
    public async Task ProcessReceipt_Should_Save_Receipt_And_OutboxMessage_Atomically()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .ConfigureWarnings(x => x.Ignore(Microsoft.EntityFrameworkCore.Diagnostics.InMemoryEventId.TransactionIgnoredWarning))
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var receiptFileServiceMock = new Mock<IReceiptFileService>();
        
        string? error = null;
        receiptFileServiceMock.Setup(s => s.TryValidate(It.IsAny<IFormFile>(), out error)).Returns(true);
        receiptFileServiceMock.Setup(s => s.SimulateUploadAsync(It.IsAny<IFormFile>())).ReturnsAsync("http://example.com/file.jpg");

        var controller = new ReceiptsController(dbContext, receiptFileServiceMock.Object);
        
        var request = new CreateReceiptRequest
        {
            UserId = Guid.NewGuid(),
            Amount = 100,
            Currency = "USD",
            ReceiptDate = DateOnly.FromDateTime(DateTime.Today),
            File = new Mock<IFormFile>().Object
        };

        // Act
        var result = await controller.ProcessReceipt(request);

        // Assert
        var acceptedResult = Assert.IsType<AcceptedResult>(result);
        
        // Check Receipt exists
        var receipt = await dbContext.Receipts.FirstOrDefaultAsync();
        Assert.NotNull(receipt);
        Assert.Equal(request.UserId, receipt.UserId);

        // Check OutboxMessage exists
        var outboxMessage = await dbContext.OutboxMessages.FirstOrDefaultAsync();
        Assert.NotNull(outboxMessage);
        Assert.Equal(OutboxStatus.New, outboxMessage.Status);
        Assert.Contains(receipt.Id.ToString(), outboxMessage.Payload);
    }
}
