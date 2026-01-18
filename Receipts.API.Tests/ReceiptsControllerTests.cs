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

    [Fact]
    public async Task GetReceipts_Should_Return_Only_Receipts_For_Specified_User()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var targetUserId = Guid.NewGuid();
        var otherUserId = Guid.NewGuid();

        // Add receipts for target user
        dbContext.Receipts.Add(new Receipt { Id = Guid.NewGuid(), UserId = targetUserId, Amount = 100, Currency = "USD", ReceiptDate = DateOnly.FromDateTime(DateTime.Today) });
        dbContext.Receipts.Add(new Receipt { Id = Guid.NewGuid(), UserId = targetUserId, Amount = 200, Currency = "EUR", ReceiptDate = DateOnly.FromDateTime(DateTime.Today) });

        // Add receipt for other user
        dbContext.Receipts.Add(new Receipt { Id = Guid.NewGuid(), UserId = otherUserId, Amount = 300, Currency = "GBP", ReceiptDate = DateOnly.FromDateTime(DateTime.Today) });
        await dbContext.SaveChangesAsync();

        var receiptFileServiceMock = new Mock<IReceiptFileService>();
        var controller = new ReceiptsController(dbContext, receiptFileServiceMock.Object);

        var request = new GetReceiptsRequest { UserId = targetUserId, Page = 1, PageSize = 10 };

        // Act
        var result = await controller.GetReceipts(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponse<ReceiptResponse>>(okResult.Value);
        Assert.Equal(2, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.All(response.Items, item => Assert.Equal(targetUserId, item.UserId));
    }

    [Fact]
    public async Task GetReceipts_Should_Return_Correct_Pagination()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var userId = Guid.NewGuid();

        // Add 5 receipts
        for (int i = 0; i < 5; i++)
        {
            dbContext.Receipts.Add(new Receipt
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Amount = 100 + i,
                Currency = "USD",
                ReceiptDate = DateOnly.FromDateTime(DateTime.Today),
                CreatedAt = DateTime.UtcNow.AddMinutes(i)
            });
        }
        await dbContext.SaveChangesAsync();

        var receiptFileServiceMock = new Mock<IReceiptFileService>();
        var controller = new ReceiptsController(dbContext, receiptFileServiceMock.Object);

        var request = new GetReceiptsRequest { UserId = userId, Page = 1, PageSize = 2 };

        // Act
        var result = await controller.GetReceipts(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponse<ReceiptResponse>>(okResult.Value);
        Assert.Equal(5, response.TotalCount);
        Assert.Equal(2, response.Items.Count);
        Assert.Equal(1, response.Page);
        Assert.Equal(2, response.PageSize);
        Assert.Equal(3, response.TotalPages);
        Assert.True(response.HasNextPage);
        Assert.False(response.HasPreviousPage);
    }

    [Fact]
    public async Task GetReceipts_Should_Return_Empty_Result_For_User_With_No_Receipts()
    {
        // Arrange
        var options = new DbContextOptionsBuilder<ReceiptsDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        using var dbContext = new ReceiptsDbContext(options);

        var userId = Guid.NewGuid();

        var receiptFileServiceMock = new Mock<IReceiptFileService>();
        var controller = new ReceiptsController(dbContext, receiptFileServiceMock.Object);

        var request = new GetReceiptsRequest { UserId = userId, Page = 1, PageSize = 10 };

        // Act
        var result = await controller.GetReceipts(request);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PagedResponse<ReceiptResponse>>(okResult.Value);
        Assert.Equal(0, response.TotalCount);
        Assert.Empty(response.Items);
        Assert.Equal(0, response.TotalPages);
        Assert.False(response.HasNextPage);
        Assert.False(response.HasPreviousPage);
    }
}
